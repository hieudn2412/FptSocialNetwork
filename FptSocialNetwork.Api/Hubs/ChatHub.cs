using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FptSocialNetwork.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly MyDbContext _dbContext;

        public ChatHub(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId.Value));
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinConversation(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                throw new HubException("Unauthorized.");
            }

            var isMember = await IsConversationMemberAsync(conversationId, userId.Value);

            if (!isMember)
            {
                throw new HubException("Forbidden.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroup(conversationId));
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroup(conversationId));
        }

        public async Task StartTyping(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                throw new HubException("Unauthorized.");
            }

            var isMember = await IsConversationMemberAsync(conversationId, userId.Value);
            if (!isMember)
            {
                throw new HubException("Forbidden.");
            }

            await Clients.GroupExcept(GetConversationGroup(conversationId), new[] { Context.ConnectionId })
                .SendAsync("TypingUpdated", new
                {
                    conversationId,
                    userId = userId.Value,
                    userName = GetCurrentUserName(),
                    isTyping = true
                });
        }

        public async Task StopTyping(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                throw new HubException("Unauthorized.");
            }

            var isMember = await IsConversationMemberAsync(conversationId, userId.Value);
            if (!isMember)
            {
                throw new HubException("Forbidden.");
            }

            await Clients.GroupExcept(GetConversationGroup(conversationId), new[] { Context.ConnectionId })
                .SendAsync("TypingUpdated", new
                {
                    conversationId,
                    userId = userId.Value,
                    userName = GetCurrentUserName(),
                    isTyping = false
                });
        }

        public static string GetConversationGroup(int conversationId) => $"conversation-{conversationId}";

        public static string GetUserGroup(int userId) => $"user-{userId}";

        private async Task<bool> IsConversationMemberAsync(int conversationId, int userId)
        {
            return await _dbContext.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
        }

        private int? GetCurrentUserId()
        {
            var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Context.User?.FindFirstValue(ClaimTypes.Name);
            return int.TryParse(raw, out var id) ? id : null;
        }

        private string GetCurrentUserName()
        {
            return Context.User?.FindFirstValue(ClaimTypes.Name)
                   ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Name)
                   ?? string.Empty;
        }
    }
}
