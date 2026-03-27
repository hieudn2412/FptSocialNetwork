using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public interface IConversationApiService
    {
        Task<ApiResponse<List<ConversationListItemDto>>> GetConversationsAsync(string? tab = null, string? keyword = null);
        Task<ApiResponse<ConversationDetailDto>> GetConversationDetailAsync(int conversationId);
        Task<ApiResponse<ConversationDetailDto>> CreateConversationAsync(CreateConversationRequest request);
        Task<ApiResponse<ConversationDetailDto>> GetOrCreateDirectConversationAsync(int targetUserId);
        Task<ApiResponse<ConversationDetailDto>> UpdateConversationSettingsAsync(int conversationId, string? name, string? avatarUrl);
        Task<ApiResponse<ConversationDetailDto>> AddMembersAsync(int conversationId, List<int> memberUserIds);
        Task<ApiResponse<object>> MarkConversationAsReadAsync(int conversationId);
    }
}
