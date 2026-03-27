public class MessageAttachment
{
    public long Id { get; set; }
    public long MessageId { get; set; }
    public string FileName { get; set; }
    public string FileUrl { get; set; }
    public string AttachmentType { get; set; }

    public virtual Message Message { get; set; }
}