using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public sealed class HCaptchaClient
{
    private readonly HttpClient _http;
    private readonly string _secret;
    private readonly string? _sitekey;

    // Default types (demo): plain
    private const string Plain = "plain";

     private readonly ILogger<HCaptchaClient> _logger;

    public HCaptchaClient(IConfiguration cfg, ILogger<HCaptchaClient> logger)
    {
        _http = new HttpClient();
        _secret = cfg["HCAPTCHA_SECRET"] ?? throw new InvalidOperationException("HCAPTCHA_SECRET missing");
        _sitekey = cfg["HCAPTCHA_SITEKEY"];
        _logger = logger;
    }

    public async Task<HCaptchaVerifyResponse> EvaluateAsync(
        HCaptchaEvaluateDto dto,
        string? remoteIp,
        CancellationToken ct)
    {
        _logger.LogInformation("HCaptchaClient.EvaluateAsync called");

        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Token))
            throw new ArgumentException("HCaptchaEvaluateDto.Token is required.", nameof(dto));

        var form = new Dictionary<string, string>
        {
            ["secret"] = _secret,
            ["response"] = dto.Token
        };

        // recommended if available
        if (!string.IsNullOrWhiteSpace(remoteIp))
            form["remoteip"] = remoteIp;

        // optional but recommended in enterprise setups
        if (!string.IsNullOrWhiteSpace(_sitekey))
            form["sitekey"] = _sitekey!;

        // Behavior type (case-sensitive)
        var behaviorType = dto.BehaviorType.ToApiValue();
        form["behavior_type"] = behaviorType;

        if (dto.BehaviorSuccess.HasValue)
            form["behavior_success"] = dto.BehaviorSuccess.Value ? "true" : "false";

        // User Journeys identity inputs (all optional)
        if (!string.IsNullOrWhiteSpace(dto.UserId))
        {
            form["user_id"] = dto.UserId!;
            form["user_id_type"] = Plain;
        }

        if (!string.IsNullOrWhiteSpace(dto.SessionId))
        {
            form["session_id"] = dto.SessionId!;
            form["session_id_type"] = Plain;
        }

        if (!string.IsNullOrWhiteSpace(dto.UserEmail))
        {
            form["user_email"] = dto.UserEmail!;
            form["user_email_type"] = Plain;
        }

        if (dto.EventData != null)
        {
            if (string.IsNullOrWhiteSpace(dto.EventData.BehaviorType))
                dto.EventData.BehaviorType = behaviorType;
            form["event_data"] = JsonSerializer.Serialize(
                dto.EventData,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                }
            );
        }

        // post_verify (optional)
        if (dto.PostVerify)
            form["post_verify"] = "true";
            
        _logger.LogInformation(
            "HCaptcha siteverify start | behavior={BehaviorType} postVerify={PostVerify} userId={HasUserId} sessionId={HasSessionId} email={HasEmail} eventData={HasEventData}",
            behaviorType,
            dto.PostVerify,
            !string.IsNullOrWhiteSpace(dto.UserId),
            !string.IsNullOrWhiteSpace(dto.SessionId),
            !string.IsNullOrWhiteSpace(dto.UserEmail),
            dto.EventData != null
        );

        _logger.LogInformation("HCaptcha siteverify request form: {Form}", JsonSerializer.Serialize(form));


        using var resp = await _http.PostAsync(
            "https://api.hcaptcha.com/siteverify",
            new FormUrlEncodedContent(form),
            ct
        );

        var json = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogInformation("HCaptcha siteverify response: {Response}", json);

        var parsed = JsonSerializer.Deserialize<HCaptchaVerifyResponse>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        ) ?? new HCaptchaVerifyResponse { Success = false };

        parsed.Raw = json;
        parsed.HttpStatus = (int)resp.StatusCode;
       

        return parsed;
    }
}

