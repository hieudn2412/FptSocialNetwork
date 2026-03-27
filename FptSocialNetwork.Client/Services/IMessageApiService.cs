using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public interface IMessageApiService
    {
        Task<ApiResponse<List<MessageDto>>> GetMessagesAsync(int conversationId, int page = 1, int pageSize = 30);
        Task<ApiResponse<MessageSearchResultDto>> SearchMessagesAsync(int conversationId, string keyword, int page = 1, int pageSize = 30);
        Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageRequest request);
        Task<ApiResponse<MessageDto>> EditMessageAsync(long messageId, string content);
        Task<ApiResponse<MessageDto>> ToggleReactionAsync(long messageId, string reactionType);
        Task<ApiResponse<MessageDto>> UnsendMessageAsync(long messageId);
        Task<ApiResponse<object>> DeleteForMeAsync(long messageId);
    }
}
