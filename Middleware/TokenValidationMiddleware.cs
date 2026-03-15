using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;
        private const string AuthorizationHeaderName = "Authorization";
        private const string BearerScheme = "Bearer ";
        
        // Routes that don't require authentication
        private static readonly string[] AllowedUnauthenticatedRoutes = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/openapi/v1.json",
            "/scalar",
            "/swagger",
            "/health"
        };

        public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
        {
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

            // Check if route requires authentication
            if (!RequiresAuthentication(path))
            {
                await _next(context);
                return;
            }

            var token = ExtractToken(context);

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Missing authorization token for request to {Path}", path);
                await ReturnUnauthorizedResponse(context, "Missing authorization token");
                return;
            }

            if (!tokenService.ValidateToken(token, out var principal))
            {
                _logger.LogWarning("Invalid authorization token for request to {Path}", path);
                await ReturnUnauthorizedResponse(context, "Invalid or expired token");
                return;
            }

            // Attach the principal to the request context for use in controllers
            context.User = principal!;
            var requestId = context.Items["RequestId"]?.ToString() ?? "N/A";
            _logger.LogInformation("Token validated successfully. User: {UserId}, RequestId: {RequestId}",
                principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown", requestId);

            await _next(context);
        }

        private static bool RequiresAuthentication(string path)
        {
            foreach (var allowedRoute in AllowedUnauthenticatedRoutes)
            {
                if (path.StartsWith(allowedRoute.ToLower()))
                {
                    return false;
                }
            }

            // All other routes require authentication
            return true;
        }

        private static string? ExtractToken(HttpContext context)
        {
            var authHeader = context.Request.Headers[AuthorizationHeaderName].ToString();

            if (string.IsNullOrEmpty(authHeader))
            {
                return null;
            }

            if (!authHeader.StartsWith(BearerScheme, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return authHeader.Substring(BearerScheme.Length).Trim();
        }

        private static Task ReturnUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Message = message,
                StatusCode = 401,
                Timestamp = DateTime.UtcNow,
                RequestId = context.Items["RequestId"]?.ToString()
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}