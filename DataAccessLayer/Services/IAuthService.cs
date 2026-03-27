using DataAccessLayer.Services.Models;

namespace DataAccessLayer.Services
{
    public interface IAuthService
    {
        Task<AuthServiceResult> RegisterAsync(RegisterRequest request);
        Task<AuthServiceResult> LoginAsync(LoginRequest request);
        Task<AuthServiceResult> LoginWithGoogleAsync(string idToken);
    }
}
