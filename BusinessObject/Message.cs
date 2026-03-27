public class Message
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public long? ReplyToMessageId { get; set; }
    public string Content { get; set; }
    public string MessageType { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeletedForEveryone { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual Conversation Conversation { get; set; }
    public virtual User Sender { get; set; }
    public virtual Message ReplyToMessage { get; set; }
    public virtual ICollection<Message> Replies { get; set; }
    public virtual ICollection<MessageAttachment> Attachments { get; set; }
    public virtual ICollection<MessageReaction> Reactions { get; set; }
    public virtual ICollection<MessageHidden> HiddenByUsers { get; set; }
}
