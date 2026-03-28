using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public interface IUserApiService
    {
        Task<ApiResponse<List<UserSearchItemDto>>> SearchUsersAsync(string keyword);
        Task<ApiResponse<MyProfileDto>> GetMyProfileAsync();
        Task<ApiResponse<MyProfileDto>> GetProfileAsync(int userId);
        Task<ApiResponse<MyProfileDto>> UpdateMyProfileAsync(UpdateMyProfileRequest request);
        Task<ApiResponse<List<FollowUserDto>>> GetMyFollowersAsync(int take = 30);
        Task<ApiResponse<List<FollowUserDto>>> GetFollowersAsync(int userId, int take = 30);
        Task<ApiResponse<List<FollowUserDto>>> GetMyFollowingAsync(int take = 30);
        Task<ApiResponse<FollowToggleResultDto>> ToggleFollowAsync(int targetUserId);
        Task<ApiResponse<bool>> GetFollowStatusAsync(int targetUserId);
    }
}
