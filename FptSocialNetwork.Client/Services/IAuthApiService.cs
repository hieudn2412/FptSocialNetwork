using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public interface IAuthApiService
    {
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request);
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    }
}
