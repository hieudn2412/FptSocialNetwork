public class Post
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int PostStatusId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public long? SharedFromPostId { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual PostStatus PostStatus { get; set; } = null!;
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual Post? SharedFromPost { get; set; }
    public virtual ICollection<Post> SharedByPosts { get; set; } = new List<Post>();
}
