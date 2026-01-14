using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public sealed class AuthService
{
    private readonly AppConfig _cfg;
    public AuthService(AppConfig cfg) => _cfg = cfg;

    public string CreateToken(string userId, string email)
    {
        // Demo JWT: a signed, time-limited token that will be stored in the "access" HttpOnly cookie.
        // Subsequent requests read this cookie and validate it to recover the user identity (ClaimsPrincipal).
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            // Standard-ish subject and email, plus a simple "uid" claim used by the demo code.
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("uid", userId)
        };

        var token = new JwtSecurityToken(
            _cfg.JwtIssuer,
            _cfg.JwtAudience,
            claims,
            // Expiration is what enforces "logged in" duration in the demo (independent from cookie max-age).
            expires: DateTime.UtcNow.AddMinutes(_cfg.JwtMinutes),
            signingCredentials: creds
        );

        // Serialized JWT string that becomes the cookie value.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? Validate(string token)
    {
        try
        {
            // Validates signature + issuer + audience + expiration and returns the claims identity.
            return new JwtSecurityTokenHandler().ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _cfg.JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _cfg.JwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.JwtSecret)),
                    ValidateLifetime = true
                }, out _);
        }
        catch
        {
            // Any validation failure is treated as "not authenticated" in the demo.
            return null;
        }
    }
}
