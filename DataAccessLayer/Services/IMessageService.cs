using DataAccessLayer.Services.Models;

namespace DataAccessLayer.Services
{
    public interface IMessageService
    {
        Task<List<MessageDto>> GetMessagesAsync(int conversationId, int currentUserId, int page, int pageSize);
        Task<MessageSearchResultDto> SearchMessagesAsync(int conversationId, int currentUserId, string keyword, int page, int pageSize);
        Task<MessageDto> SendMessageAsync(SendMessageRequest request);
        Task<MessageDto> EditMessageAsync(long messageId, int userId, string content);
        Task<MessageDto> ToggleReactionAsync(long messageId, int userId, string reactionType);
        Task<MessageDto> UnsendMessageAsync(long messageId, int userId);
        Task<int> DeleteForMeAsync(long messageId, int userId);
    }
}
