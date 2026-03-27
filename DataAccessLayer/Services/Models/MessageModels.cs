namespace DataAccessLayer.Services.Models
{
    public class SeenByDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class MessageReactionDto
    {
        public string ReactionType { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsMine { get; set; }
    }

    public class MessageReceiptDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "delivered";
        public DateTime? LastReadAt { get; set; }
    }

    public class MessageReplyPreviewDto
    {
        public long MessageId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsDeletedForEveryone { get; set; }
    }

    public class MessageSearchItemDto
    {
        public long MessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatarUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    public class MessageSearchResultDto
    {
        public int TotalCount { get; set; }
        public List<MessageSearchItemDto> Items { get; set; } = new();
    }

    public class MessageAttachmentDto
    {
        public long Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty;
    }

    public class MessageDto
    {
        public long Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatarUrl { get; set; } = string.Empty;
        public long? ReplyToMessageId { get; set; }
        public MessageReplyPreviewDto? ReplyTo { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeletedForEveryone { get; set; }
        public DateTime? DeletedAt { get; set; }
        public List<MessageAttachmentDto> Attachments { get; set; } = new();
        public List<MessageReactionDto> Reactions { get; set; } = new();
        public List<SeenByDto> SeenBy { get; set; } = new();
        public List<MessageReceiptDto> Receipts { get; set; } = new();
    }

    public class SendMessageAttachmentRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty;
    }

    public class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public long? ReplyToMessageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public List<SendMessageAttachmentRequest> Attachments { get; set; } = new();
    }

    public class MessageReactionRequest
    {
        public string ReactionType { get; set; } = "like";
    }

    public class MessageEditRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
