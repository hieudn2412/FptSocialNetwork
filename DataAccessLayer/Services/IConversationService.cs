using DataAccessLayer.Services.Models;

namespace DataAccessLayer.Services
{
    public interface IConversationService
    {
        Task<List<ConversationListItemDto>> GetConversationsAsync(int userId, string? tab, string? keyword);
        Task<ConversationDetailDto?> GetConversationDetailAsync(int conversationId, int userId);
        Task<ConversationDetailDto> CreateConversationAsync(int currentUserId, CreateConversationRequest request);
        Task<ConversationDetailDto> GetOrCreateDirectConversationAsync(int currentUserId, int targetUserId);
        Task<ConversationDetailDto> UpdateConversationSettingsAsync(int conversationId, int userId, string? name, string? avatarUrl);
        Task<ConversationDetailDto> AddMembersAsync(int conversationId, int userId, List<int> memberUserIds);
        Task<DateTime> MarkConversationAsReadAsync(int conversationId, int userId);
        Task DeleteConversationAsync(int conversationId, int userId);
    }
}
