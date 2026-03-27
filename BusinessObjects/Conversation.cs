public class Conversation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string AvatarUrl { get; set; }

    public virtual ICollection<ConversationMember> Members { get; set; }
    public virtual ICollection<Message> Messages { get; set; }
}