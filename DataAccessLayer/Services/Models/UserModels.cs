namespace DataAccessLayer.Services.Models
{
    public class FollowUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime FollowedAt { get; set; }
    }

    public class FollowToggleResultDto
    {
        public int TargetUserId { get; set; }
        public bool IsFollowing { get; set; }
    }

    public class UserSearchItemDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class MyProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string School { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string RelationshipStatus { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
    }

    public class UpdateMyProfileRequest
    {
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? School { get; set; }
        public string? Gender { get; set; }
        public string? RelationshipStatus { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
