namespace DataAccessLayer.Services.Models
{
    public class ConversationListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public int? LastSenderId { get; set; }
        public string LastSenderName { get; set; } = string.Empty;
        public DateTime? LastSentAt { get; set; }
        public int UnreadCount { get; set; }
        public bool HasUnread { get; set; }
    }

    public class ConversationMemberDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class ConversationDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public List<ConversationMemberDto> Members { get; set; } = new();
    }

    public class CreateConversationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "group";
        public string AvatarUrl { get; set; } = string.Empty;
        public List<int> MemberUserIds { get; set; } = new();
    }

    public class UpdateConversationSettingsRequest
    {
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class AddConversationMembersRequest
    {
        public List<int> MemberUserIds { get; set; } = new();
    }
}
