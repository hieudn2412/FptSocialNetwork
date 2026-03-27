public class MessageReaction
{
    public long Id { get; set; }
    public long MessageId { get; set; }
    public int UserId { get; set; }
    public string ReactionType { get; set; }
    public DateTime ReactedAt { get; set; }

    public virtual Message Message { get; set; }
    public virtual User User { get; set; }
}
