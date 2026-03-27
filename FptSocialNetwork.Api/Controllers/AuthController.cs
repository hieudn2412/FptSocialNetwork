using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataAccessLayer.Services;
using DataAccessLayer.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FptSocialNetwork.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.IsSuccess || result.User is null)
            {
                return BadRequest(result.ErrorMessage ?? "Register failed.");
            }

            return Ok(BuildAuthResponse(result.User));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.IsSuccess || result.User is null)
            {
                return Unauthorized(result.ErrorMessage ?? "Login failed.");
            }

            return Ok(BuildAuthResponse(result.User));
        }

        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var result = await _authService.LoginWithGoogleAsync(request.IdToken);
            if (!result.IsSuccess || result.User is null)
            {
                return Unauthorized(result.ErrorMessage ?? "Google login failed.");
            }

            return Ok(BuildAuthResponse(result.User));
        }

        private AuthResponse BuildAuthResponse(User user)
        {
            var secret = _configuration["Jwt:Key"] ?? "super-secret-key-change-this";
            var issuer = _configuration["Jwt:Issuer"] ?? "FptSocialNetwork.Api";
            var audience = _configuration["Jwt:Audience"] ?? "FptSocialNetwork.Client";
            var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var m) ? m : 120;

            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt,
                User = new AuthUserDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email
                }
            };
        }
    }
}
