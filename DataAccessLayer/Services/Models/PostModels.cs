namespace DataAccessLayer.Services.Models
{
    public class CreatePostRequest
    {
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? PostStatusId { get; set; }
        public string? MediaUrl { get; set; }
    }

    public class AddCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class SharePostRequest
    {
        public string? Content { get; set; }
        public int? PostStatusId { get; set; }
    }

    public class UpdatePostRequest
    {
        public string Content { get; set; } = string.Empty;
        public int? PostStatusId { get; set; }
    }

    public class DeletePostResultDto
    {
        public long PostId { get; set; }
    }

    public class PostCommentDto
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public int UserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorAvatarUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ToggleReactionResultDto
    {
        public long PostId { get; set; }
        public bool IsLiked { get; set; }
        public int LikeCount { get; set; }
    }

    public class PostDto
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorAvatarUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MediaUrl { get; set; } = string.Empty;
        public int PostStatusId { get; set; }
        public string PostStatusName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsAuthorFollowedByCurrentUser { get; set; }
        public List<PostCommentDto> RecentComments { get; set; } = new();
        public long? SourcePostId { get; set; }
        public int? SourceUserId { get; set; }
        public string SourceAuthorName { get; set; } = string.Empty;
        public string SourceAuthorAvatarUrl { get; set; } = string.Empty;
        public string SourceContent { get; set; } = string.Empty;
        public string SourceMediaUrl { get; set; } = string.Empty;
        public DateTime? SourceCreatedAt { get; set; }
        public bool SourcePostDeleted { get; set; }
    }
}
