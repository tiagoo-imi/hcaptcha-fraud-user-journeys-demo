using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Http;

public static class Helpers
{
    public static string BuildUserId(string? fullName)
    {
        var guid = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(fullName))
            return guid;

        // Normalize accents
        var normalized = fullName
            .Trim()
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var cat = Char.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var clean = sb
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToUpperInvariant();

        // Split name and take at most first 2 parts
        var parts = clean
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .ToArray();

        if (parts.Length == 0)
            return guid;

        // Keep only letters (A–Z)
        var hint = Regex.Replace(string.Concat(parts), "[^A-Z]", "");

        if (string.IsNullOrWhiteSpace(hint))
            return guid;

        // Limit hint length to avoid huge RowKeys
        const int maxHintLength = 12;
        if (hint.Length > maxHintLength)
            hint = hint.Substring(0, maxHintLength);

        return $"{hint}_{guid}";
    }

    public static string? GetClientIp(HttpRequestData req)
    {
        // Standard proxy header (can be a list: "client, proxy1, proxy2")
        if (req.Headers.TryGetValues("X-Forwarded-For", out var xff))
        {
            var raw = xff.FirstOrDefault();
            var ip = raw?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip)) return ip;
        }

        // Sometimes used by proxies
        if (req.Headers.TryGetValues("X-Real-IP", out var xri))
        {
            var ip = xri.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip)) return ip;
        }

        // Azure Front Door / App Gateway variants (optional)
        if (req.Headers.TryGetValues("X-Azure-ClientIP", out var xaz))
        {
            var ip = xaz.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip)) return ip;
        }

        if (IsRunningLocal())
        {
            return "127.0.0.1";
        }

        return null;
    }

    public static bool IsRunningLocal()
    {
        var v = Environment.GetEnvironmentVariable("RUNNING_LOCAL");
        return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
    }
}
