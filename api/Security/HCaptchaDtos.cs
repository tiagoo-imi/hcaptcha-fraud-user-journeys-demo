using System.Text.Json.Serialization;

using System.Reflection;
using System.Text.Json;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ApiValueAttribute : Attribute
{
    public string Value { get; }
    public ApiValueAttribute(string value) => Value = value;
}

public enum BehaviorType
{
    [ApiValue("")]
    None,
    [ApiValue("login")]
    Login,

    [ApiValue("signup")]
    Signup,

    [ApiValue("password_reset")]
    PasswordReset,

    [ApiValue("pageview")]
    Pageview,

    [ApiValue("add_to_cart")]
    AddToCart,

    [ApiValue("giveaway")]
    Giveaway,

    [ApiValue("purchase")]
    Purchase,

    [ApiValue("contact_form")]
    ContactForm,

    [ApiValue("account_update")]
    AccountUpdate,

    [ApiValue("user_post")]
    UserPost,

    [ApiValue("user_comment")]
    UserComment,

    [ApiValue("post_interaction")]
    PostInteraction,

    [ApiValue("other")]
    Other,

    [ApiValue("username_recovery")]
    UsernameRecovery,

    [ApiValue("email_verify")]
    EmailVerify,

    [ApiValue("phone_verify")]
    PhoneVerify,

    [ApiValue("other_verify")]
    OtherVerify,

    [ApiValue("user_logout")]
    UserLogout,

    [ApiValue("forced_logout")]
    ForcedLogout,

    [ApiValue("session_timeout")]
    SessionTimeout,

    [ApiValue("session_end_other")]
    SessionEndOther,

    [ApiValue("account_linking")]
    AccountLinking
}

public static class BehaviorTypeExtensions
{
    public static string ToApiValue(this BehaviorType behavior)
    {
        var member = typeof(BehaviorType).GetMember(behavior.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<ApiValueAttribute>();
        return attr?.Value ?? "";
    }
}


public class HCaptchaEvaluateDto
{
    // Required
    public string Token { get; set; } = default!;            // hcaptcha token (response)

    // Identity (User Journeys)
    public string? UserId { get; set; }                      // your user id (plain)
    public string? SessionId { get; set; }                   // your session id (plain)
    public string? UserEmail { get; set; }                   // plain email (demo)

    // Behavior labeling
    public BehaviorType BehaviorType { get; set; } = BehaviorType.None;
    public bool? BehaviorSuccess { get; set; }               // optional (useful for failed login / declined purchase)

    // Fraud Protection (only for Purchase normally)
    public FraudEventDataDto? EventData { get; set; }        // optional, serialized as JSON

    // Post-verify control
    public bool PostVerify { get; set; }                     // if true => send post_verify=true
}

public sealed class FraudEventDataDto
{
    // Required by docs inside event_data
    [JsonPropertyName("behavior_type")]
    public string BehaviorType { get; set; } = default!; // "purchase", "signup" etc (case-sensitive)

    [JsonPropertyName("transaction_data")]
    public TransactionDataDto? TransactionData { get; set; }
}

public sealed class TransactionDataDto
{
    // Available only for post_siteverify/post_verify requests (per your doc snippet)
    [JsonPropertyName("reason_code")]
    public string? ReasonCode { get; set; } // e.g. "CHARGEBACK"

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; } // supports blinded_<field>

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; } // "credit_card"

    [JsonPropertyName("payment_network")]
    public string? PaymentNetwork { get; set; } // "visa"

    [JsonPropertyName("in_person")]
    public bool? InPerson { get; set; }

    [JsonPropertyName("merchant_account_id")]
    public string? MerchantAccountId { get; set; } // supports blinded_<field>

    [JsonPropertyName("card_bin")]
    public string? CardBin { get; set; } // supports blinded_<field>

    [JsonPropertyName("card_last_four")]
    public string? CardLastFour { get; set; } // supports blinded_<field>

    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; } // "USD"

    [JsonPropertyName("value")]
    public decimal? Value { get; set; } // supports blinded_<field>

    [JsonPropertyName("shipping_value")]
    public decimal? ShippingValue { get; set; } // supports blinded_<field>

    [JsonPropertyName("shipping_address")]
    public AddressDto? ShippingAddress { get; set; }

    [JsonPropertyName("billing_address")]
    public AddressDto? BillingAddress { get; set; }

    [JsonPropertyName("user")]
    public TransactionUserDto? User { get; set; }

    // capped at 100 (regra do backend do hCaptcha)
    [JsonPropertyName("items")]
    public List<TransactionItemDto>? Items { get; set; }

    [JsonPropertyName("gateway_result")]
    public GatewayResultDto? GatewayResult { get; set; }
}

public sealed class AddressDto
{
    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; } // supports blinded_<field>

    [JsonPropertyName("address_1")]
    public string? Address1 { get; set; } // supports blinded_<field>

    [JsonPropertyName("address_2")]
    public string? Address2 { get; set; } // supports blinded_<field>

    [JsonPropertyName("city")]
    public string? City { get; set; } // supports blinded_<field>

    [JsonPropertyName("sub_region")]
    public string? SubRegion { get; set; } // supports blinded_<field> (ex: "FL")

    [JsonPropertyName("region_code")]
    public string? RegionCode { get; set; } // "USA" (no exemplo)

    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; } // supports blinded_<field>
}

public sealed class TransactionUserDto
{
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; } // supports blinded_<field>

    // unix ts (segundos) segundo o snippet
    [JsonPropertyName("account_created_ts")]
    public long? AccountCreatedTs { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; } // supports blinded_<field>

    [JsonPropertyName("email_domain")]
    public string? EmailDomain { get; set; } // plaintext sugerido no doc

    [JsonPropertyName("email_verified")]
    public bool? EmailVerified { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; } // supports blinded_<field>

    [JsonPropertyName("phone_verified")]
    public bool? PhoneVerified { get; set; }
}

public sealed class TransactionItemDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; } // supports blinded_<field>

    [JsonPropertyName("value")]
    public decimal? Value { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("merchant_account_id")]
    public string? MerchantAccountId { get; set; } // supports blinded_<field>

    [JsonPropertyName("merchant_host")]
    public string? MerchantHost { get; set; }
}

public sealed class GatewayResultDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; } // "stripe"

    [JsonPropertyName("gateway_response_code")]
    public string? GatewayResponseCode { get; set; } // "SUCCESS"

    [JsonPropertyName("avs_response_code")]
    public string? AvsResponseCode { get; set; } // "Y" or null

    [JsonPropertyName("cvv_response_code")]
    public string? CvvResponseCode { get; set; } // "M" or null (NUNCA o CVV)

    [JsonPropertyName("3dsecure_passed")]
    public bool? ThreeDSecurePassed { get; set; } // bool or null
}

public class HCaptchaVerifyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error-codes")]
    public string[]? ErrorCodes { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("score_reason")]
    public string[]? ScoreReason { get; set; }

    // Fraud Protection
    [JsonPropertyName("fraud_predictions")]
    public FraudPredictionsDto? FraudPredictions { get; set; }

    // Similarity (Account Defense / Journeys)
    [JsonPropertyName("similarity")]
    public double? Similarity { get; set; }

    // Some integrations return "similarity-indicators"
    [JsonPropertyName("similarity-indicators")]
    public string[]? SimilarityIndicators { get; set; }

    // Risk Insights (keep raw)
    [JsonPropertyName("risk_insights")]
    public JsonElement? RiskInsights { get; set; }

    // Handy: extract ekey if present
    public string? EventKey { get; set; }

    // Debug extras (your code)
    public string? Raw { get; set; }
    public int? HttpStatus { get; set; }
}

public class FraudPredictionsDto
{
    [JsonPropertyName("fraud_score")]
    public double? FraudScore { get; set; }

    [JsonPropertyName("fraud_score_reason")]
    public string[]? FraudScoreReason { get; set; }
}

