using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FptSocialNetwork.Api.Hubs
{
    [Authorize]
    public class SocialHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId.Value));
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, FeedGroup);
            await base.OnConnectedAsync();
        }

        public static string GetUserGroup(int userId) => $"user-{userId}";

        public const string FeedGroup = "social-feed";

        private int? GetCurrentUserId()
        {
            var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Context.User?.FindFirstValue(ClaimTypes.Name);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
