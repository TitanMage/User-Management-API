using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UserManagementAPI.Services
{
    public interface ITokenService
    {
        string GenerateToken(Guid userId, string email, string firstName, string lastName);
        bool ValidateToken(string token, out ClaimsPrincipal? principal);
        string? GetUserIdFromToken(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private const string TokenKey = "JwtSettings:SecretKey";
        private const string TokenIssuer = "JwtSettings:Issuer";
        private const string TokenAudience = "JwtSettings:Audience";
        private const string TokenExpirationMinutes = "JwtSettings:ExpirationMinutes";

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateToken(Guid userId, string email, string firstName, string lastName)
        {
            try
            {
                var secretKey = _configuration[TokenKey];
                var issuer = _configuration[TokenIssuer];
                var audience = _configuration[TokenAudience];
                var expirationMinutes = int.TryParse(_configuration[TokenExpirationMinutes], out var exp) ? exp : 60;

                if (string.IsNullOrEmpty(secretKey))
                {
                    throw new InvalidOperationException("JWT secret key not configured.");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, $"{firstName} {lastName}"),
                    new Claim("firstName", firstName),
                    new Claim("lastName", lastName),
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                    signingCredentials: credentials);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {UserId}", userId);
                throw;
            }
        }

        public bool ValidateToken(string token, out ClaimsPrincipal? principal)
        {
            principal = null;

            try
            {
                var secretKey = _configuration[TokenKey];
                var issuer = _configuration[TokenIssuer];
                var audience = _configuration[TokenAudience];

                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogWarning("JWT secret key not configured.");
                    return false;
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                SecurityToken validatedToken;
                principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                return true;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("JWT token has expired: {ExceptionMessage}", ex.Message);
                return false;
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning("JWT token has invalid signature: {ExceptionMessage}", ex.Message);
                return false;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("JWT token validation failed: {ExceptionMessage}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating JWT token");
                return false;
            }
        }

        public string? GetUserIdFromToken(string token)
        {
            if (ValidateToken(token, out var principal))
            {
                var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim?.Value;
            }

            return null;
        }
    }
}