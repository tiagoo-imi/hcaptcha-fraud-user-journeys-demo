using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Microsoft.Extensions.Logging;

public sealed class DemoFunctions
{
    private readonly TableStore _store;
    private readonly PasswordHasher _hasher;
    private readonly AuthService _auth;
    private readonly CookieService _cookies;
    private readonly HCaptchaClient _hcaptcha;
    private readonly ILogger<DemoFunctions> _log;

    public DemoFunctions(
        TableStore s,
        PasswordHasher h,
        AuthService a,
        CookieService c,
        HCaptchaClient hcaptcha,
        ILogger<DemoFunctions> logger)
    {
        _store = s;
        _hasher = h;
        _auth = a;
        _cookies = c;
        _hcaptcha = hcaptcha;
        _log = logger;
    }

    [Function("SessionStart")]
    public async Task<HttpResponseData> SessionStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/start")] HttpRequestData req)
    {
        // Ensures storage tables exist before any read/write. Keeps demo self-contained on a fresh deploy.
        await _store.EnsureAsync();

        // "sid" is the local demo session identifier used to correlate User Journey events across requests.
        var sid = CookieService.Get(req, "sid") ?? Guid.NewGuid().ToString("N");
        await _store.TouchSession(sid);

        var res = req.Json(new BaseResponseDto(true));

        // Persist sid for 7 days so the journey survives refreshes and short revisits.
        _cookies.Set(res, "sid", sid, 60 * 60 * 24 * 7);
        return res;
    }

    record SessionEndReq(string? HCaptchaToken);

    [Function("SessionEnd")]
    public async Task<HttpResponseData> SessionEnd(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "session/end")] HttpRequestData req)
    {
        await _store.EnsureAsync();

        var body = await req.ReadJson<SessionEndReq>();

        // Access cookie is a demo JWT; if present we can attach user identity to the logout behavior.
        var sid = CookieService.Get(req, "sid");
        var access = CookieService.Get(req, "access");
        var principal = access != null ? _auth.Validate(access) : null;
        var userId = principal?.FindFirst("uid")?.Value;

        _log.LogInformation("Ending session {SessionId} for user {UserId}", sid, userId);

        string? userEmail = principal?.FindFirst("email")?.Value;
        userEmail = userEmail?.Trim().ToLowerInvariant();

        // Used to forward a best-effort client IP to the evaluation endpoint for risk signals.
        var remoteIp = Helpers.GetClientIp(req);

        if (!string.IsNullOrWhiteSpace(body?.HCaptchaToken))
        {
            // Optional evaluation on logout so the journey shows a clean "user_logout" behavior event.
            var dto = new HCaptchaEvaluateDto
            {
                Token = body!.HCaptchaToken!,
                BehaviorType = BehaviorType.UserLogout,
                BehaviorSuccess = true,
                SessionId = sid,
                UserId = userId,
                UserEmail = userEmail,
                PostVerify = false
            };

            await _hcaptcha.EvaluateAsync(dto, remoteIp, CancellationToken.None);
        }

        // Marks the local demo session as ended (analytics/debug); does not impact hCaptcha.
        if (!string.IsNullOrWhiteSpace(sid))
            await _store.EndSession(sid);

        var res = req.Json(new BaseResponseDto(true));

        // Clear both correlation (sid) and auth (access) to fully reset the demo state.
        _cookies.Clear(res, "sid");
        _cookies.Clear(res, "access");

        return res;
    }

    record Cred(string Email, string Password, string? FullName, string? HCaptchaToken);

    [Function("Signup")]
    public async Task<HttpResponseData> Signup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "signup")] HttpRequestData req)
    {
        await _store.EnsureAsync();

        var b = await req.ReadJson<Cred>();
        if (b == null ||
            string.IsNullOrWhiteSpace(b.Email) ||
            string.IsNullOrWhiteSpace(b.Password) ||
            string.IsNullOrWhiteSpace(b.HCaptchaToken))
        {
            return req.Json(new BaseResponseDto(false, "Email, password and hcaptchaToken are required."));
        }

        var emailNorm = b.Email.Trim().ToLowerInvariant();
        var sid = CookieService.Get(req, "sid");
        var remoteIp = Helpers.GetClientIp(req);

        // PRE verification: evaluate the behavior before account creation (no userId yet).
        var preDto = new HCaptchaEvaluateDto
        {
            Token = b.HCaptchaToken,
            BehaviorType = BehaviorType.Signup,
            SessionId = sid,
            UserEmail = emailNorm,
            PostVerify = false
        };

        var pre = await _hcaptcha.EvaluateAsync(preDto, remoteIp, CancellationToken.None);

        // EventKey extraction + decision analysis are demo-friendly helpers to surface what the platform decided.
        pre.EventKey = HCaptchaResponseAnalyzer.TryExtractEventKey(pre);
        var preDecision = HCaptchaResponseAnalyzer.Analyze(pre);

        if (!preDecision.Passed)
            return req.Json(new SignupResponseDto(false, "hCaptcha verification failed.",
                HCaptchaResponseDto.FromHCaptchaDecision(preDecision)));

        var existing = await _store.GetUserByEmail(emailNorm);
        if (existing != null)
            return req.Json(new SignupResponseDto(false, "User already exists.", null));

        // Create the local demo user only after pre-verification passes.
        var user = await _store.CreateUser(emailNorm, b.FullName ?? "", _hasher.Hash(b.Password));
        var userId = user.RowKey;

        // POST verification: re-evaluate the same token but now binding the newly created identity.
        // This is useful for demos to show identity correlation in journeys right after signup.
        var postDto = new HCaptchaEvaluateDto
        {
            Token = b.HCaptchaToken,
            BehaviorType = BehaviorType.Signup,
            SessionId = sid,
            UserId = userId,
            UserEmail = emailNorm,
            PostVerify = true
        };

        var post = await _hcaptcha.EvaluateAsync(postDto, remoteIp, CancellationToken.None);

        post.EventKey = HCaptchaResponseAnalyzer.TryExtractEventKey(post);
        var postDecision = HCaptchaResponseAnalyzer.Analyze(post);

        if (!postDecision.Passed)
            return req.Json(new SignupResponseDto(false, "hCaptcha verification failed.",
                HCaptchaResponseDto.FromHCaptchaDecision(postDecision)));

        // Issue auth cookie so subsequent actions can be tied to the same user in the journey.
        var jwt = _auth.CreateToken(user.RowKey, user.Email);

        var res = req.Json(new SignupResponseDto(true, null,
            HCaptchaResponseDto.FromHCaptchaDecision(postDecision))
        {
            UserId = user.RowKey,
            Email = user.Email,
            FullName = user.FullName
        });
        _cookies.Set(res, "access", jwt, 3600);

        // Persist the mapping sid -> userId to enrich the journey even when future requests only have sid.
        if (!string.IsNullOrWhiteSpace(sid))
            await _store.BindSession(sid, user.RowKey);

        return res;
    }

    [Function("Login")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "login")] HttpRequestData req)
    {
        await _store.EnsureAsync();

        var b = await req.ReadJson<Cred>();
        if (b == null ||
            string.IsNullOrWhiteSpace(b.Email) ||
            string.IsNullOrWhiteSpace(b.Password) ||
            string.IsNullOrWhiteSpace(b.HCaptchaToken))
        {
            return req.Json(new LoginResponseDto(false, "Email, password and hcaptchaToken are required.", null));
        }

        var emailNorm = b.Email.Trim().ToLowerInvariant();
        var sid = CookieService.Get(req, "sid");
        var remoteIp = Helpers.GetClientIp(req);

        _log.LogInformation("Remote IP for login attempt: {RemoteIp}", remoteIp);

        // Load user first so we can attach a stable userId to the behavior event if password matches.
        var u = await _store.GetUserByEmail(emailNorm);
        if (u == null)
            return req.Json(new LoginResponseDto(false, "Invalid username or password.", null));

        var passOk = _hasher.Verify(b.Password, u.PasswordHash);
        if (!passOk)
            return req.Json(new LoginResponseDto(false, "Invalid username or password.", null));

        // Evaluate login behavior and bind it to sid + user identity for the journey view.
        var dto = new HCaptchaEvaluateDto
        {
            Token = b.HCaptchaToken!,
            BehaviorType = BehaviorType.Login,
            SessionId = sid,
            UserId = u.RowKey,
            UserEmail = emailNorm,
            BehaviorSuccess = true,
            PostVerify = false
        };

        var hc = await _hcaptcha.EvaluateAsync(dto, remoteIp, CancellationToken.None);
        var decision = HCaptchaResponseAnalyzer.Analyze(hc);

        if (!decision.Passed)
            return req.Json(new LoginResponseDto(false, "hCaptcha verification failed.",
                HCaptchaResponseDto.FromHCaptchaDecision(decision)));

        // Success: set auth cookie and bind sid -> userId so downstream events are correlated.
        var jwt = _auth.CreateToken(u.RowKey, u.Email);
        var res = req.Json(new LoginResponseDto(true, null,
            HCaptchaResponseDto.FromHCaptchaDecision(decision))
        {
            UserId = u.RowKey,
            Email = u.Email,
            FullName = u.FullName
        });
        _cookies.Set(res, "access", jwt, 3600);

        if (!string.IsNullOrWhiteSpace(sid))
            await _store.BindSession(sid!, u.RowKey);

        _log.LogInformation("User {UserId} logged in successfully.", u.RowKey);
        _log.LogInformation(res.ToString());

        return res;
    }

    record CartAddReq(
        string? ItemId,
        int? Quantity,
        decimal? UnitPrice,
        string? HCaptchaToken
    );

    [Function("CartAdd")]
    public async Task<HttpResponseData> CartAdd(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cart/add")] HttpRequestData req)
    {
        await _store.EnsureAsync();

        // Cart actions are protected by demo auth so the behavior event can be tied to a real user.
        var access = CookieService.Get(req, "access");
        var principal = access != null ? _auth.Validate(access) : null;
        var userId = principal?.FindFirst("uid")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return req.Json(new AddToCardResponseDto(false, "Unauthorized"), HttpStatusCode.Unauthorized);

        var b = await req.ReadJson<CartAddReq>();
        if (b == null || string.IsNullOrWhiteSpace(b.HCaptchaToken))
            return req.Json(new BaseResponseDto(false, "hcaptchaToken is required."), HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(b.ItemId))
            return req.Json(new BaseResponseDto(false, "itemId is required."), HttpStatusCode.BadRequest);

        // Normalize input to keep transaction event clean and deterministic.
        var qty = (b.Quantity is null or <= 0) ? 1 : b.Quantity.Value;
        var unitPrice = b.UnitPrice ?? 0m;

        var sid = CookieService.Get(req, "sid");
        var remoteIp = Helpers.GetClientIp(req);

        var user = await _store.GetUserById(userId);
        var emailNorm = user?.Email?.Trim().ToLowerInvariant();

        // Deterministic-enough transactionId for demo tracing (sid + user + item + timestamp).
        var cartTxnId = $"{sid ?? "nosid"}:{userId}:{b.ItemId}:{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";

        // EventData/TransactionData is what makes this a "fraud" / commerce-style behavior event in journeys.
        var eventData = new FraudEventDataDto
        {
            BehaviorType = BehaviorType.AddToCart.ToApiValue(),
            TransactionData = new TransactionDataDto
            {
                TransactionId = cartTxnId,
                Value = unitPrice * qty,
                Items = new List<TransactionItemDto>
                {
                    new TransactionItemDto
                    {
                        Name = b.ItemId,
                        Quantity = qty,
                        Value = unitPrice,
                    }
                },
                User = new TransactionUserDto
                {
                    AccountId = userId,
                    Email = emailNorm,
                    EmailDomain = emailNorm != null && emailNorm.Contains("@")
                        ? emailNorm.Split('@')[1]
                        : null,
                }
            }
        };

        var dto = new HCaptchaEvaluateDto
        {
            Token = b.HCaptchaToken!,
            BehaviorType = BehaviorType.AddToCart,
            SessionId = sid,
            UserId = userId,
            UserEmail = emailNorm,
            PostVerify = false,
            EventData = eventData
        };

        var hc = await _hcaptcha.EvaluateAsync(dto, remoteIp, CancellationToken.None);
        var decision = HCaptchaResponseAnalyzer.Analyze(hc);

        if (!decision.Passed)
        {
            return req.Json(new AddToCardResponseDto(false, "hCaptcha verification failed.",
                HCaptchaResponseDto.FromHCaptchaDecision(decision)));
        }

        return req.Json(new AddToCardResponseDto(true, "", HCaptchaResponseDto.FromHCaptchaDecision(decision))
        {
            ItemId = b.ItemId,
            Quantity = qty
        });
    }

    record PasswordResetReq(
        string? CurrentPassword,
        string? NewPassword,
        string? HCaptchaToken
    );

    [Function("PasswordReset")]
    public async Task<HttpResponseData> PasswordReset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "password/reset")] HttpRequestData req)
    {
        await _store.EnsureAsync();

        var access = CookieService.Get(req, "access");
        var principal = access != null ? _auth.Validate(access) : null;
        var userId = principal?.FindFirst("uid")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return req.Json(new AddToCardResponseDto(false, "Unauthorized"), HttpStatusCode.Unauthorized);

        var b = await req.ReadJson<PasswordResetReq>();
        if (b == null ||
            string.IsNullOrWhiteSpace(b.CurrentPassword) ||
            string.IsNullOrWhiteSpace(b.NewPassword) ||
            string.IsNullOrWhiteSpace(b.HCaptchaToken))
        {
            return req.Json(new FunctionResponseDto(false, "Current password, new password and hcaptchaToken are required.", null));
        }

        var user = await _store.GetUserById(userId);
        var emailNorm = user?.Email?.Trim().ToLowerInvariant();

        var sid = CookieService.Get(req, "sid");
        var remoteIp = Helpers.GetClientIp(req);

        // Validate current password locally first; only then spend an evaluation call for the reset behavior.
        var passOk = _hasher.Verify(b.CurrentPassword, user.PasswordHash);
        if (!passOk)
            return req.Json(new FunctionResponseDto(false, "Invalid current password.", null));

        var preDto = new HCaptchaEvaluateDto
        {
            Token = b.HCaptchaToken,
            BehaviorType = BehaviorType.PasswordReset,
            SessionId = sid,
            UserId = userId,
            UserEmail = emailNorm,
            PostVerify = false
        };

        var pre = await _hcaptcha.EvaluateAsync(preDto, remoteIp, CancellationToken.None);
        pre.EventKey = HCaptchaResponseAnalyzer.TryExtractEventKey(pre);
        var preDecision = HCaptchaResponseAnalyzer.Analyze(pre);

        if (!preDecision.Passed)
            return req.Json(new FunctionResponseDto(false, "hCaptcha verification failed.",
                HCaptchaResponseDto.FromHCaptchaDecision(preDecision)));

        await _store.UpdateUserPassword(userId, _hasher.Hash(b.NewPassword));

        return req.Json(new FunctionResponseDto(true, null,
            HCaptchaResponseDto.FromHCaptchaDecision(preDecision)));
    }

    record CheckoutReq(
        string? HCaptchaToken,

        decimal? Value,
        string? CurrencyCode,
        string? PaymentMethod,
        string? PaymentNetwork,
        string? CardBin,
        string? CardLastFour,

        decimal? ShippingValue,

        AddressDto? ShippingAddress,
        AddressDto? BillingAddress,

        List<TransactionItemDto>? Items
    );

    [Function("Checkout")]
    public async Task<HttpResponseData> Checkout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkout")] HttpRequestData req)
    {
        await _store.EnsureAsync();

        var access = CookieService.Get(req, "access");
        var principal = access != null ? _auth.Validate(access) : null;
        var userId = principal?.FindFirst("uid")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return req.Json(new AddToCardResponseDto(false, "Unauthorized"), HttpStatusCode.Unauthorized);

        var b = await req.ReadJson<CheckoutReq>();
        if (b == null)
            return req.Json(new FunctionResponseDto(false, "Invalid data.", null));

        var user = await _store.GetUserById(userId);
        var emailNorm = user?.Email?.Trim().ToLowerInvariant();

        var sid = CookieService.Get(req, "sid");
        var remoteIp = Helpers.GetClientIp(req);

        // Demo transaction id; short, readable, and stable enough for screenshots/logs.
        var txId = "TX_" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

        // Checkout is modeled as a "purchase" behavior with full transaction metadata to enrich journeys.
        var postDto = new HCaptchaEvaluateDto
        {
            Token = b.HCaptchaToken!,
            BehaviorType = BehaviorType.Purchase,
            SessionId = sid,
            UserId = userId,
            UserEmail = emailNorm,
            PostVerify = false,
            EventData = new FraudEventDataDto
            {
                // Explicit API value to match downstream dashboards/queries.
                BehaviorType = "purchase",
                TransactionData = new TransactionDataDto
                {
                    TransactionId = txId,

                    // In a real integration, these come from the payment processor.
                    PaymentMethod = "credit_card",
                    PaymentNetwork = b.PaymentNetwork,
                    CardBin = b.CardBin,
                    CardLastFour = b.CardLastFour,

                    CurrencyCode = b.CurrencyCode?.Trim().ToUpperInvariant(),
                    Value = b.Value,
                    ShippingValue = b.ShippingValue,

                    ShippingAddress = b.ShippingAddress,
                    BillingAddress = b.BillingAddress,

                    User = new TransactionUserDto
                    {
                        AccountId = userId,
                        Email = emailNorm,
                        EmailDomain = !string.IsNullOrEmpty(emailNorm) && emailNorm.Contains('@') ? emailNorm.Split('@')[1] : null,
                    },

                    // Guardrail to keep payload bounded for demos/logging.
                    Items = (b.Items?.Count ?? 0) > 100 ? b.Items!.Take(100).ToList() : b.Items
                }
            }
        };

        var post = await _hcaptcha.EvaluateAsync(postDto, remoteIp, CancellationToken.None);
        post.EventKey = HCaptchaResponseAnalyzer.TryExtractEventKey(post);
        var postDecision = HCaptchaResponseAnalyzer.Analyze(post);

        if (!postDecision.Passed)
            return req.Json(new FunctionResponseDto(false, "hCaptcha verification failed.",
                HCaptchaResponseDto.FromHCaptchaDecision(postDecision)));

        return req.Json(new FunctionResponseDto(true, null,
            HCaptchaResponseDto.FromHCaptchaDecision(postDecision)));
    }
}
