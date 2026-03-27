public class Message
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; }
    public string MessageType { get; set; }
    public DateTime SentAt { get; set; }

    public virtual Conversation Conversation { get; set; }
    public virtual User Sender { get; set; }
    public virtual ICollection<MessageAttachment> Attachments { get; set; }
}