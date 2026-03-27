using DataAccessLayer.Services.Models;

namespace DataAccessLayer.Services
{
    public interface IUserService
    {
        Task<List<UserSearchItemDto>> SearchUsersAsync(int currentUserId, string keyword);
    }
}
