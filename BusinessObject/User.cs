public class User
{
    public int Id { get; set; }
    public int? UserRoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    public virtual UserRole? UserRole { get; set; }
    public virtual UserProfile? UserProfile { get; set; }
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
    public virtual ICollection<MessageHidden> MessageHiddens { get; set; } = new List<MessageHidden>();
    public virtual ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual ICollection<Follow> FollowingRelations { get; set; } = new List<Follow>();
    public virtual ICollection<Follow> FollowerRelations { get; set; } = new List<Follow>();
}
