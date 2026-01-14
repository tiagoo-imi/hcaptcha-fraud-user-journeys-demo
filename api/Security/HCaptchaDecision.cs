public sealed class HCaptchaDecision
{
    public bool Passed { get; init; }

    public double? RiskScore { get; init; }
    public double? FraudScore { get; init; }

    public string[] ErrorCodes { get; init; } = Array.Empty<string>();
    public string[] SimilarityIndicators { get; init; } = Array.Empty<string>();

    public double? Similarity { get; init; }
    public string? EventKey { get; init; }

    public string Explain()
    {
        var parts = new List<string>
        {
            Passed ? "siteverify=PASS" : "siteverify=FAIL"
        };


        //checking risk only if present and valid (>=0) as -1 means not calculated
        if (RiskScore.HasValue && RiskScore.Value >= 0) parts.Add($"risk={RiskScore:0.00}");
        if (FraudScore.HasValue) parts.Add($"fraud={FraudScore:0.00}");
        //checking similarity only if present, as -1 means not calculated
        if (Similarity.HasValue && Similarity.Value >= 0) parts.Add($"similarity={Similarity:0.00}");
        if (!string.IsNullOrWhiteSpace(EventKey)) parts.Add($"ekey={EventKey}");

        if (SimilarityIndicators.Length > 0)
            parts.Add($"indicators=[{string.Join(",", SimilarityIndicators)}]");

        if (ErrorCodes.Length > 0)
            parts.Add($"errors=[{string.Join(",", ErrorCodes)}]");

        return string.Join(" | ", parts);
    }
}
