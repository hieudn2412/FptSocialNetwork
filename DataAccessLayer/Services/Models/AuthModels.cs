namespace DataAccessLayer.Services.Models
{
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class AuthUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public AuthUserDto User { get; set; } = new();
    }

    public class AuthServiceResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
    }
}
