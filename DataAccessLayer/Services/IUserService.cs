using DataAccessLayer.Services.Models;

namespace DataAccessLayer.Services
{
    public interface IUserService
    {
        Task<List<UserSearchItemDto>> SearchUsersAsync(int currentUserId, string keyword);
        Task<MyProfileDto?> GetMyProfileAsync(int userId);
        Task<MyProfileDto?> GetProfileAsync(int userId);
        Task<MyProfileDto> UpdateMyProfileAsync(int userId, UpdateMyProfileRequest request);
        Task<List<FollowUserDto>> GetFollowersAsync(int userId, int take = 30);
        Task<List<FollowUserDto>> GetFollowingAsync(int userId, int take = 30);
        Task<FollowToggleResultDto> ToggleFollowAsync(int currentUserId, int targetUserId);
        Task<bool> IsFollowingAsync(int currentUserId, int targetUserId);
    }
}
