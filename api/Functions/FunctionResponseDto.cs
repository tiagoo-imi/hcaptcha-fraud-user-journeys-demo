using System.Text.Json.Serialization;

public class BaseResponseDto
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    public BaseResponseDto()
    {
        
    }
    public BaseResponseDto(bool ok, string? message = null)
    {
        Ok = ok;
        Message = message;
    }
}

public class FunctionResponseDto : BaseResponseDto
{
    [JsonPropertyName("hcaptcha_response")]
    public HCaptchaResponseDto? HCaptchaResponse { get; set; }

    public FunctionResponseDto()
    {
        
    }

    public FunctionResponseDto(bool ok, string? message, HCaptchaResponseDto? hcaptchaResponse) : base(ok, message)
    {
        HCaptchaResponse = hcaptchaResponse;
    }
}

public class SignupResponseDto : FunctionResponseDto
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    public SignupResponseDto()
    {
        
    }

    public SignupResponseDto(bool ok, string? message, HCaptchaResponseDto? hcaptchaResponse) 
        : base(ok, message, hcaptchaResponse)
    {
    }
}

public class LoginResponseDto : FunctionResponseDto
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    public LoginResponseDto()
    {
        
    }

    public LoginResponseDto(bool ok, string? message, HCaptchaResponseDto? hcaptchaResponse) 
        : base(ok, message, hcaptchaResponse)
    {
    }
}

public class AddToCardResponseDto : FunctionResponseDto
{
    [JsonPropertyName("item_id")]
    public string? ItemId { get; set; }
    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    public AddToCardResponseDto()
    {
        
    }

    public AddToCardResponseDto(bool ok, string? message = null, HCaptchaResponseDto? hcaptchaResponse = null) 
        : base(ok, message, hcaptchaResponse)
    {
    }
}

public class HCaptchaResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("hcaptcha_approved")]
    public bool HCaptchaApproved => !BotDetected && !FraudDetected && !AccountTakeoverSuspected;
    [JsonPropertyName("bot_detected")]
    public bool BotDetected { get; set; }

    [JsonPropertyName("risk_score")]
    public double? RiskScore { get; set; }

    [JsonPropertyName("fraud_detected")]
    public bool FraudDetected { get; set; }
    [JsonPropertyName("fraud_score")]
    public double? FraudScore { get; set; }

    [JsonPropertyName("account_takeover_suspected")]
    public bool AccountTakeoverSuspected { get; set; }
    [JsonPropertyName("similarity")]
    public double? Similarity { get; set; }
    [JsonPropertyName("similarity_indicators")]
    public string[]? SimilarityIndicators { get; set; }

    public HCaptchaResponseDto()
    {
        
    }

    public static HCaptchaResponseDto FromHCaptchaDecision(HCaptchaDecision decision)
    {
        return new HCaptchaResponseDto
        {
            Success = decision.Passed,
            BotDetected = decision.RiskScore.HasValue && decision.RiskScore.Value >= 0.80,
            RiskScore = decision.RiskScore ?? 0,
            FraudDetected = decision.FraudScore.HasValue && decision.FraudScore.Value >= 0.80,
            FraudScore = decision.FraudScore ?? 0,
            AccountTakeoverSuspected = decision.Similarity.HasValue && decision.Similarity.Value >= 0 && decision.Similarity.Value < 0.70,
            Similarity = decision.Similarity ?? 0,
            SimilarityIndicators = decision.SimilarityIndicators
        };
    }
}