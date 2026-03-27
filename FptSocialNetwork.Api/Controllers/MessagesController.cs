using System.Security.Claims;
using DataAccess;
using DataAccessLayer.Services;
using DataAccessLayer.Services.Models;
using FptSocialNetwork.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FptSocialNetwork.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly MyDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(
            IMessageService messageService,
            MyDbContext dbContext,
            IHubContext<ChatHub> hubContext)
        {
            _messageService = messageService;
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<MessageDto>>> GetMessages(
            [FromQuery] int conversationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var messages = await _messageService.GetMessagesAsync(conversationId, userId.Value, page, pageSize);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<MessageSearchResultDto>> SearchMessages(
            [FromQuery] int conversationId,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var messages = await _messageService.SearchMessagesAsync(conversationId, userId.Value, q ?? string.Empty, page, pageSize);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            request.SenderId = userId.Value;
            try
            {
                var created = await _messageService.SendMessageAsync(request);
                await _hubContext.Clients
                    .Group(ChatHub.GetConversationGroup(created.ConversationId))
                    .SendAsync("MessageReceived", created);
                await BroadcastConversationUpdatedAsync(created.ConversationId, created.Id, created.SentAt);
                return CreatedAtAction(nameof(GetMessages), new { conversationId = created.ConversationId }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{messageId:long}/reactions")]
        public async Task<ActionResult<MessageDto>> ToggleReaction(long messageId, [FromBody] MessageReactionRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var updated = await _messageService.ToggleReactionAsync(messageId, userId.Value, request.ReactionType);
                await _hubContext.Clients
                    .Group(ChatHub.GetConversationGroup(updated.ConversationId))
                    .SendAsync("MessageUpdated", updated);
                await BroadcastConversationUpdatedAsync(updated.ConversationId, updated.Id, updated.SentAt);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{messageId:long}/edit")]
        public async Task<ActionResult<MessageDto>> EditMessage(long messageId, [FromBody] MessageEditRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var updated = await _messageService.EditMessageAsync(messageId, userId.Value, request.Content);
                await _hubContext.Clients
                    .Group(ChatHub.GetConversationGroup(updated.ConversationId))
                    .SendAsync("MessageUpdated", updated);
                await BroadcastConversationUpdatedAsync(updated.ConversationId, updated.Id, updated.SentAt);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{messageId:long}/unsend")]
        public async Task<ActionResult<MessageDto>> Unsend(long messageId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var updated = await _messageService.UnsendMessageAsync(messageId, userId.Value);
                await _hubContext.Clients
                    .Group(ChatHub.GetConversationGroup(updated.ConversationId))
                    .SendAsync("MessageUpdated", updated);
                await BroadcastConversationUpdatedAsync(updated.ConversationId, updated.Id, updated.SentAt);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{messageId:long}/hide")]
        public async Task<IActionResult> DeleteForMe(long messageId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var conversationId = await _messageService.DeleteForMeAsync(messageId, userId.Value);
                await _hubContext.Clients
                    .Group(ChatHub.GetUserGroup(userId.Value))
                    .SendAsync("ConversationUpdated", new { conversationId });
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }

        private async Task BroadcastConversationUpdatedAsync(int conversationId, long messageId, DateTime sentAt)
        {
            var memberUserIds = await _dbContext.ConversationMembers
                .Where(cm => cm.ConversationId == conversationId)
                .Select(cm => cm.UserId)
                .Distinct()
                .ToListAsync();

            if (memberUserIds.Count == 0)
            {
                return;
            }

            var targetGroups = memberUserIds
                .Select(ChatHub.GetUserGroup)
                .ToList();

            await _hubContext.Clients
                .Groups(targetGroups)
                .SendAsync("ConversationUpdated", new
                {
                    conversationId,
                    messageId,
                    sentAt
                });
        }
    }
}
