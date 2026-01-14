using Microsoft.Azure.Functions.Worker.Http;

public sealed class CookieService
{
    private readonly AppConfig _cfg;
    public CookieService(AppConfig cfg) => _cfg = cfg;

    public void Set(HttpResponseData res, string name, string value, int seconds)
    {
        // "Secure" ensures the cookie is only sent over HTTPS.
        // It must be disabled for local HTTP development, otherwise the browser will not persist the cookie.
        var secure = _cfg.UseSecureCookies;

        var c = $"{name}={Uri.EscapeDataString(value)}; Path=/; Max-Age={seconds}; SameSite=Lax"
              + (secure ? "; Secure" : "")
              + "; HttpOnly";

        res.Headers.Add("Set-Cookie", c);
    }

    public static string? Get(HttpRequestData req, string name)
    {
        if (!req.Headers.TryGetValues("Cookie", out var cookies)) return null;
        foreach (var h in cookies)
            foreach (var p in h.Split(';'))
            {
                var kv = p.Trim().Split('=', 2);
                if (kv.Length == 2 && kv[0] == name) return Uri.UnescapeDataString(kv[1]);
            }
        return null;
    }

    public void Clear(HttpResponseData res, string name, bool secure = true)
    {
        var secureFlag = secure ? "; Secure" : "";
        res.Headers.Add(
            "Set-Cookie",
            $"{name}=; Max-Age=0; Expires=Thu, 01 Jan 1970 00:00:00 GMT; Path=/; HttpOnly; SameSite=Lax{secureFlag}"
        );
    }
}
