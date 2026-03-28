using DataAccessLayer.Services.Models;
using Microsoft.AspNetCore.Http;

namespace FptSocialNetwork.Client.Models
{
    public class FeedUserViewModel
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class HomeFeedViewModel
    {
        public List<PostDto> Posts { get; set; } = new();
        public List<FeedUserViewModel> FollowingUsers { get; set; } = new();
        public string ComposerPlaceholder { get; set; } = "Bạn đang nghĩ gì?";
        public string PostError { get; set; } = string.Empty;
        public string ProfileError { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentUserAvatarUrl { get; set; } = string.Empty;
        public int CurrentUserId { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string School { get; set; } = string.Empty;
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public bool IsOwnProfile { get; set; } = true;
        public int ViewedUserId { get; set; }
        public bool IsFollowingViewedUser { get; set; }
        public string RealtimeToken { get; set; } = string.Empty;
        public string SocialHubUrl { get; set; } = string.Empty;
    }

    public class CreatePostInputModel
    {
        public string Content { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
        public int? PostStatusId { get; set; }
        public string Source { get; set; } = "index";
    }

    public class UpdateProfileInputModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string School { get; set; } = string.Empty;
    }

    public class UpdateAvatarInputModel
    {
        public IFormFile? AvatarImage { get; set; }
    }
}
