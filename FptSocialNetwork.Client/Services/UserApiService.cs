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

        public async Task<ApiResponse<MyProfileDto>> GetMyProfileAsync()
        {
            return await _apiClient.GetAsync<MyProfileDto>("api/users/me");
        }

        public async Task<ApiResponse<MyProfileDto>> UpdateMyProfileAsync(UpdateMyProfileRequest request)
        {
            return await _apiClient.PostAsync<UpdateMyProfileRequest, MyProfileDto>("api/users/me/profile", request);
        }

        public async Task<ApiResponse<List<FollowUserDto>>> GetMyFollowersAsync(int take = 30)
        {
            return await _apiClient.GetAsync<List<FollowUserDto>>($"api/users/me/followers?take={take}");
        }

        public async Task<ApiResponse<List<FollowUserDto>>> GetMyFollowingAsync(int take = 30)
        {
            return await _apiClient.GetAsync<List<FollowUserDto>>($"api/users/me/following?take={take}");
        }

        public async Task<ApiResponse<FollowToggleResultDto>> ToggleFollowAsync(int targetUserId)
        {
            return await _apiClient.PostAsync<FollowToggleResultDto>($"api/users/{targetUserId}/follow-toggle");
        }

        public async Task<ApiResponse<List<UserSearchItemDto>>> SearchUsersAsync(string keyword)
        {
            return await _apiClient.GetAsync<List<UserSearchItemDto>>(
                $"api/users/search?q={Uri.EscapeDataString(keyword)}");
        }
    }
}
