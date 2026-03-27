public class User
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string AvatarUrl { get; set; }

    public virtual ICollection<Message> Messages { get; set; }
    public virtual ICollection<ConversationMember> ConversationMembers { get; set; }
}