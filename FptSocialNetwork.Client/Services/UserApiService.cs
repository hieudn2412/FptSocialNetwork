using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public class UserApiService : IUserApiService
    {
        private readonly IApiClient _apiClient;

        public UserApiService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResponse<List<UserSearchItemDto>>> SearchUsersAsync(string keyword)
        {
            return await _apiClient.GetAsync<List<UserSearchItemDto>>(
                $"api/users/search?q={Uri.EscapeDataString(keyword)}");
        }
    }
}
