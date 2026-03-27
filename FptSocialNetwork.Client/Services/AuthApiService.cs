using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public class AuthApiService : IAuthApiService
    {
        private readonly IApiClient _apiClient;

        public AuthApiService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            return await _apiClient.PostAsync<LoginRequest, AuthResponse>("api/auth/login", request);
        }

        public async Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request)
        {
            return await _apiClient.PostAsync<GoogleLoginRequest, AuthResponse>("api/auth/google", request);
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            return await _apiClient.PostAsync<RegisterRequest, AuthResponse>("api/auth/register", request);
        }
    }
}
