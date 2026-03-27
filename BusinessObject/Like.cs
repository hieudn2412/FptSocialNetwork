public class Like
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Post Post { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
