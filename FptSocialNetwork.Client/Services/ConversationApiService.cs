using DataAccessLayer.Services.Models;

namespace FptSocialNetwork.Client.Services
{
    public class ConversationApiService : IConversationApiService
    {
        private readonly IApiClient _apiClient;

        public ConversationApiService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<ApiResponse<List<ConversationListItemDto>>> GetConversationsAsync(
            string? tab = null,
            string? keyword = null)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(tab))
            {
                queryParams.Add($"tab={Uri.EscapeDataString(tab)}");
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                queryParams.Add($"q={Uri.EscapeDataString(keyword)}");
            }

            var query = queryParams.Count == 0
                ? "api/conversations"
                : $"api/conversations?{string.Join("&", queryParams)}";

            return await _apiClient.GetAsync<List<ConversationListItemDto>>(query);
        }

        public async Task<ApiResponse<ConversationDetailDto>> GetConversationDetailAsync(int conversationId)
        {
            return await _apiClient.GetAsync<ConversationDetailDto>($"api/conversations/{conversationId}");
        }

        public async Task<ApiResponse<ConversationDetailDto>> CreateConversationAsync(CreateConversationRequest request)
        {
            return await _apiClient.PostAsync<CreateConversationRequest, ConversationDetailDto>("api/conversations", request);
        }

        public async Task<ApiResponse<ConversationDetailDto>> GetOrCreateDirectConversationAsync(int targetUserId)
        {
            return await _apiClient.PostAsync<ConversationDetailDto>($"api/conversations/direct/{targetUserId}");
        }

        public async Task<ApiResponse<ConversationDetailDto>> UpdateConversationSettingsAsync(int conversationId, string? name, string? avatarUrl)
        {
            var request = new UpdateConversationSettingsRequest
            {
                Name = name,
                AvatarUrl = avatarUrl
            };

            return await _apiClient.PostAsync<UpdateConversationSettingsRequest, ConversationDetailDto>(
                $"api/conversations/{conversationId}/settings",
                request);
        }

        public async Task<ApiResponse<ConversationDetailDto>> AddMembersAsync(int conversationId, List<int> memberUserIds)
        {
            var request = new AddConversationMembersRequest
            {
                MemberUserIds = memberUserIds ?? new List<int>()
            };

            return await _apiClient.PostAsync<AddConversationMembersRequest, ConversationDetailDto>(
                $"api/conversations/{conversationId}/members",
                request);
        }

        public async Task<ApiResponse<object>> MarkConversationAsReadAsync(int conversationId)
        {
            return await _apiClient.PostAsync<object>($"api/conversations/{conversationId}/read");
        }

        public async Task<ApiResponse<object>> DeleteConversationAsync(int conversationId)
        {
            return await _apiClient.PostAsync<object>($"api/conversations/{conversationId}/delete");
        }
    }
}
