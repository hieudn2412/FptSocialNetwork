public class MessageHidden
{
    public long Id { get; set; }
    public long MessageId { get; set; }
    public int UserId { get; set; }
    public DateTime HiddenAt { get; set; }

    public virtual Message Message { get; set; }
    public virtual User User { get; set; }
}
