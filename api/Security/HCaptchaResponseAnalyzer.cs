using System.Text.Json;

public static class HCaptchaResponseAnalyzer
{
    public static HCaptchaDecision Analyze(HCaptchaVerifyResponse r)
    {
        if (r == null)
            return new HCaptchaDecision
            {
                Passed = false,
                ErrorCodes = ["null_response"]
            };

        var risk = r.Score;
        var fraud = r.FraudPredictions?.FraudScore;

        var indicators = r.SimilarityIndicators ?? Array.Empty<string>();
        var errors = r.ErrorCodes ?? Array.Empty<string>();

        if (!r.Success)
        {
            return new HCaptchaDecision
            {
                Passed = false,
                RiskScore = risk,
                FraudScore = fraud,
                Similarity = r.Similarity,
                SimilarityIndicators = indicators,
                ErrorCodes = errors,
                EventKey = r.EventKey
            };
        }

        return new HCaptchaDecision
        {
            Passed = true,
            RiskScore = risk,
            FraudScore = fraud,
            Similarity = r.Similarity,
            SimilarityIndicators = indicators,
            ErrorCodes = errors,
            EventKey = r.EventKey
        };
    }

    /// Extracts risk_insights.session_details.ekey if present
    public static string? TryExtractEventKey(HCaptchaVerifyResponse r)
    {
        if (r?.RiskInsights is not { } ri) return null;

        try
        {
            if (ri.ValueKind != JsonValueKind.Object) return null;
            if (!ri.TryGetProperty("session_details", out var sd)) return null;
            if (sd.ValueKind != JsonValueKind.Object) return null;
            if (!sd.TryGetProperty("ekey", out var ekey)) return null;
            if (ekey.ValueKind != JsonValueKind.String) return null;

            return ekey.GetString();
        }
        catch
        {
            return null;
        }
    }
}
