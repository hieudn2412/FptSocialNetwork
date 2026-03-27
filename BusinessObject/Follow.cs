public class Follow
{
    public long Id { get; set; }
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User Follower { get; set; } = null!;
    public virtual User Following { get; set; } = null!;
}
