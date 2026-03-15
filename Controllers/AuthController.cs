using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;
using UserManagementAPI.Repositories;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserRepository userRepository, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user and return authentication token
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<TokenResponse>> Register([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration request");
                return BadRequest(ModelState);
            }

            try
            {
                // Check if user already exists
                var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email: {Email}", dto.Email);
                    return Conflict(new { message = "A user with this email already exists." });
                }

                // Create new user
                var user = new User
                {
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Email = dto.Email.Trim(),
                    DateOfBirth = dto.DateOfBirth
                };

                var createdUser = await _userRepository.CreateAsync(user);

                // Generate token
                var token = _tokenService.GenerateToken(
                    createdUser.Id,
                    createdUser.Email,
                    createdUser.FirstName,
                    createdUser.LastName);

                _logger.LogInformation("User registered successfully. UserId: {UserId}, Email: {Email}", createdUser.Id, createdUser.Email);

                return Ok(new TokenResponse
                {
                    Token = token,
                    ExpiresIn = 3600,
                    TokenType = "Bearer",
                    User = createdUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                throw;
            }
        }

        /// <summary>
        /// Login with email and return authentication token
        /// Note: In production, you'd validate a password here
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrWhiteSpace(loginRequest.Email))
            {
                _logger.LogWarning("Login attempt with empty email");
                return BadRequest(new { message = "Email is required." });
            }

            try
            {
                // Find user by email
                var user = await _userRepository.GetByEmailAsync(loginRequest.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt for non-existent user: {Email}", loginRequest.Email);
                    return Unauthorized(new { message = "Invalid email or password." });
                }

                // In a real application, you'd verify a password here
                // For demo purposes, we're just checking if the user exists

                // Generate token
                var token = _tokenService.GenerateToken(
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName);

                _logger.LogInformation("User logged in successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

                return Ok(new TokenResponse
                {
                    Token = token,
                    ExpiresIn = 3600,
                    TokenType = "Bearer",
                    User = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                throw;
            }
        }

        /// <summary>
        /// Validate current token
        /// </summary>
        [HttpPost("validate")]
        public ActionResult<bool> ValidateToken([FromBody] TokenValidationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { message = "Token is required." });
            }

            var isValid = _tokenService.ValidateToken(request.Token, out var principal);
            
            if (isValid)
            {
                _logger.LogInformation("Token validation successful for user: {UserId}",
                    principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Unknown");
            }
            else
            {
                _logger.LogWarning("Token validation failed");
            }

            return Ok(new { isValid });
        }

        /// <summary>
        /// Refresh token (returns new token)
        /// </summary>
        [HttpPost("refresh")]
        public ActionResult<TokenResponse> RefreshToken([FromBody] TokenRefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { message = "Token is required." });
            }

            if (!_tokenService.ValidateToken(request.Token, out var principal))
            {
                _logger.LogWarning("Token refresh failed - invalid token");
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            try
            {
                var userIdClaim = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                var emailClaim = principal?.FindFirst(System.Security.Claims.ClaimTypes.Email);
                var firstNameClaim = principal?.FindFirst("firstName");
                var lastNameClaim = principal?.FindFirst("lastName");

                if (userIdClaim == null || emailClaim == null)
                {
                    return Unauthorized(new { message = "Invalid token claims." });
                }

                var newToken = _tokenService.GenerateToken(
                    Guid.Parse(userIdClaim.Value),
                    emailClaim.Value,
                    firstNameClaim?.Value ?? string.Empty,
                    lastNameClaim?.Value ?? string.Empty);

                _logger.LogInformation("Token refreshed for user: {UserId}", userIdClaim.Value);

                return Ok(new TokenResponse
                {
                    Token = newToken,
                    ExpiresIn = 3600,
                    TokenType = "Bearer"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                throw;
            }
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class TokenValidationRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class TokenRefreshRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public User? User { get; set; }
    }
}