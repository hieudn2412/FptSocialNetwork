using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public class MessageApiService : IMessageApiService
    {
        private readonly IApiClient _apiClient;

        public MessageApiService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResponse<List<MessageDto>>> GetMessagesAsync(int conversationId, int page = 1, int pageSize = 30)
        {
            return await _apiClient.GetAsync<List<MessageDto>>(
                $"api/messages?conversationId={conversationId}&page={page}&pageSize={pageSize}");
        }

        public async Task<ApiResponse<MessageSearchResultDto>> SearchMessagesAsync(int conversationId, string keyword, int page = 1, int pageSize = 30)
        {
            return await _apiClient.GetAsync<MessageSearchResultDto>(
                $"api/messages/search?conversationId={conversationId}&q={Uri.EscapeDataString(keyword)}&page={page}&pageSize={pageSize}");
        }

        public async Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageRequest request)
        {
            return await _apiClient.PostAsync<SendMessageRequest, MessageDto>("api/messages", request);
        }

        public async Task<ApiResponse<MessageDto>> EditMessageAsync(long messageId, string content)
        {
            return await _apiClient.PostAsync<MessageEditRequest, MessageDto>(
                $"api/messages/{messageId}/edit",
                new MessageEditRequest
                {
                    Content = content
                });
        }

        public async Task<ApiResponse<MessageDto>> ToggleReactionAsync(long messageId, string reactionType)
        {
            return await _apiClient.PostAsync<MessageReactionRequest, MessageDto>(
                $"api/messages/{messageId}/reactions",
                new MessageReactionRequest
                {
                    ReactionType = reactionType
                });
        }

        public async Task<ApiResponse<MessageDto>> UnsendMessageAsync(long messageId)
        {
            return await _apiClient.PostAsync<MessageDto>($"api/messages/{messageId}/unsend");
        }

        public async Task<ApiResponse<object>> DeleteForMeAsync(long messageId)
        {
            return await _apiClient.PostAsync<object>($"api/messages/{messageId}/hide");
        }
    }
}
