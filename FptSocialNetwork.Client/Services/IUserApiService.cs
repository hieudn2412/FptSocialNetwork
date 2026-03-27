using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public interface IUserApiService
    {
        Task<ApiResponse<List<UserSearchItemDto>>> SearchUsersAsync(string keyword);
    }
}
