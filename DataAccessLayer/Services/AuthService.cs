using System.Security.Cryptography;
using System.Text;
using DataAccess;
using DataAccessLayer.Services.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(MyDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthServiceResult> RegisterAsync(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existed = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
            if (existed)
            {
                return new AuthServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Email already exists."
                };
            }

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                PasswordHash = HashPassword(request.Password),
                AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl)
                    ? "https://via.placeholder.com/64"
                    : request.AvatarUrl.Trim()
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return new AuthServiceResult
            {
                IsSuccess = true,
                User = user
            };
        }

        public async Task<AuthServiceResult> LoginAsync(LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var hashedPassword = HashPassword(request.Password);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.PasswordHash == hashedPassword);

            if (user is null)
            {
                return new AuthServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid email or password."
                };
            }

            return new AuthServiceResult
            {
                IsSuccess = true,
                User = user
            };
        }

        public async Task<AuthServiceResult> LoginWithGoogleAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return new AuthServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Google token is required."
                };
            }

            var clientId = _configuration["GoogleAuth:ClientId"]?.Trim();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return new AuthServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "GoogleAuth:ClientId is not configured."
                };
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { clientId }
                    });
            }
            catch (Exception)
            {
                return new AuthServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid Google token."
                };
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return new AuthServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Google account does not provide an email."
                };
            }

            var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
            var googleAvatarUrl = string.IsNullOrWhiteSpace(payload.Picture)
                ? "https://via.placeholder.com/64"
                : payload.Picture.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user is null)
            {
                user = new User
                {
                    FullName = string.IsNullOrWhiteSpace(payload.Name) ? normalizedEmail : payload.Name.Trim(),
                    Email = normalizedEmail,
                    PasswordHash = HashPassword(Guid.NewGuid().ToString("N")),
                    AvatarUrl = googleAvatarUrl
                };

                await _context.Users.AddAsync(user);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(payload.Name))
                {
                    user.FullName = payload.Name.Trim();
                }

                user.AvatarUrl = googleAvatarUrl;
            }

            await _context.SaveChangesAsync();

            return new AuthServiceResult
            {
                IsSuccess = true,
                User = user
            };
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
