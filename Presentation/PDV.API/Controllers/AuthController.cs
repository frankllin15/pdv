using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace PDV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Simple validation for demo purposes
        // In production, validate against a user database
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { Message = "Username and password are required" });
        }

        // Demo: Accept any non-empty credentials
        // TODO: Replace with proper user authentication
        if (request.Username == "admin" && request.Password == "admin123")
        {
            var token = GenerateJwtToken(request.Username, request.StoreId);
            _logger.LogInformation("User {Username} logged in successfully from store {StoreId}",
                request.Username, request.StoreId);

            return Ok(new LoginResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "1440")),
                Username = request.Username,
                StoreId = request.StoreId
            });
        }

        _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
        return Unauthorized(new { Message = "Invalid credentials" });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        // Validate the existing token and issue a new one
        // In production, implement proper token refresh logic
        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { Message = "Token is required" });
        }

        try
        {
            var principal = ValidateToken(request.Token);
            if (principal == null)
            {
                return Unauthorized(new { Message = "Invalid token" });
            }

            var username = principal.Identity?.Name ?? "unknown";
            var storeId = principal.FindFirst("StoreId")?.Value ?? "default";

            var newToken = GenerateJwtToken(username, storeId);

            return Ok(new LoginResponse
            {
                Token = newToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "1440")),
                Username = username,
                StoreId = storeId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return Unauthorized(new { Message = "Invalid token" });
        }
    }

    private string GenerateJwtToken(string username, string? storeId)
    {
        var key = _configuration["Jwt:Key"]!;
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "1440");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("StoreId", storeId ?? "default")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        var key = _configuration["Jwt:Key"]!;
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Allow expired tokens for refresh
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }
}

public record LoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? StoreId { get; init; }
}

public record RefreshRequest
{
    public string Token { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? StoreId { get; init; }
}
