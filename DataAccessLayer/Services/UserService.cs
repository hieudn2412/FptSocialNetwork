using DataAccess;
using DataAccessLayer.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly MyDbContext _context;

        public UserService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserSearchItemDto>> SearchUsersAsync(int currentUserId, string keyword)
        {
            var normalizedKeyword = keyword.Trim().ToLowerInvariant();

            return await _context.Users
                .Where(u => u.Id != currentUserId &&
                            (u.FullName.ToLower().Contains(normalizedKeyword) ||
                             u.Email.ToLower().Contains(normalizedKeyword)))
                .OrderBy(u => u.FullName)
                .Take(20)
                .Select(u => new UserSearchItemDto
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync();
        }
    }
}
