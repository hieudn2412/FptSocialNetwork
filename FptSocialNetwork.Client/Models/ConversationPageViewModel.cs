using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Models
{
    public class ConversationPageViewModel
    {
        public int CurrentUserId { get; set; }
        public int? SelectedConversationId { get; set; }
        public string? Tab { get; set; }
        public string? Keyword { get; set; }
        public string? ErrorMessage { get; set; }
        public bool ShouldScrollToBottom { get; set; }
        public string ChatHubUrl { get; set; } = string.Empty;
        public string RealtimeToken { get; set; } = string.Empty;

        public List<ConversationListItemDto> Conversations { get; set; } = new();
        public List<UserSearchItemDto> UserSearchResults { get; set; } = new();
        public ConversationDetailDto? SelectedConversation { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
    }
}
