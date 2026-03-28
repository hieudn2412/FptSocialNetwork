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

        public async Task<MyProfileDto?> GetMyProfileAsync(int userId)
        {
            return await GetProfileAsync(userId);
        }

        public async Task<MyProfileDto?> GetProfileAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new MyProfileDto
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl,
                    Bio = u.UserProfile != null ? u.UserProfile.Bio : string.Empty,
                    City = u.UserProfile != null ? u.UserProfile.City : string.Empty,
                    School = u.UserProfile != null ? u.UserProfile.School : string.Empty,
                    Gender = u.UserProfile != null ? u.UserProfile.Gender : string.Empty,
                    RelationshipStatus = u.UserProfile != null ? u.UserProfile.RelationshipStatus : string.Empty,
                    DateOfBirth = u.UserProfile != null ? u.UserProfile.DateOfBirth : null,
                    FollowerCount = u.FollowerRelations.Count(),
                    FollowingCount = u.FollowingRelations.Count()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<MyProfileDto> UpdateMyProfileAsync(int userId, UpdateMyProfileRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (request.FullName is not null)
            {
                var normalizedFullName = request.FullName.Trim();
                if (string.IsNullOrWhiteSpace(normalizedFullName))
                {
                    throw new InvalidOperationException("Full name is required.");
                }

                user.FullName = normalizedFullName;
            }

            if (request.AvatarUrl is not null)
            {
                user.AvatarUrl = request.AvatarUrl.Trim();
            }

            var profile = user.UserProfile;
            if (profile is null)
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.UserProfiles.AddAsync(profile);
            }

            if (request.Bio is not null)
            {
                profile.Bio = request.Bio.Trim();
            }

            if (request.City is not null)
            {
                profile.City = request.City.Trim();
            }

            if (request.School is not null)
            {
                profile.School = request.School.Trim();
            }

            if (request.Gender is not null)
            {
                profile.Gender = request.Gender.Trim();
            }

            if (request.RelationshipStatus is not null)
            {
                profile.RelationshipStatus = request.RelationshipStatus.Trim();
            }

            if (request.DateOfBirth.HasValue)
            {
                profile.DateOfBirth = request.DateOfBirth.Value;
            }

            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updated = await GetProfileAsync(userId);
            if (updated is null)
            {
                throw new InvalidOperationException("Cannot load updated profile.");
            }

            return updated;
        }

        public async Task<List<FollowUserDto>> GetFollowersAsync(int userId, int take = 30)
        {
            take = NormalizeTake(take);

            return await _context.Follows
                .Where(f => f.FollowingId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Take(take)
                .Select(f => new FollowUserDto
                {
                    UserId = f.FollowerId,
                    FullName = f.Follower.FullName,
                    AvatarUrl = f.Follower.AvatarUrl,
                    FollowedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<List<FollowUserDto>> GetFollowingAsync(int userId, int take = 30)
        {
            take = NormalizeTake(take);

            return await _context.Follows
                .Where(f => f.FollowerId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Take(take)
                .Select(f => new FollowUserDto
                {
                    UserId = f.FollowingId,
                    FullName = f.Following.FullName,
                    AvatarUrl = f.Following.AvatarUrl,
                    FollowedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<FollowToggleResultDto> ToggleFollowAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId)
            {
                throw new InvalidOperationException("Bạn không thể tự theo dõi chính mình.");
            }

            var targetExists = await _context.Users.AnyAsync(u => u.Id == targetUserId);
            if (!targetExists)
            {
                throw new InvalidOperationException("Người dùng không tồn tại.");
            }

            var relation = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            var isFollowing = false;
            if (relation is null)
            {
                await _context.Follows.AddAsync(new Follow
                {
                    FollowerId = currentUserId,
                    FollowingId = targetUserId,
                    CreatedAt = DateTime.UtcNow
                });
                isFollowing = true;
            }
            else
            {
                _context.Follows.Remove(relation);
            }

            await _context.SaveChangesAsync();

            return new FollowToggleResultDto
            {
                TargetUserId = targetUserId,
                IsFollowing = isFollowing
            };
        }

        public async Task<bool> IsFollowingAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId)
            {
                return false;
            }

            return await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);
        }

        private static int NormalizeTake(int take)
        {
            if (take <= 0)
            {
                return 30;
            }

            return Math.Min(take, 200);
        }
    }
}
