using DataAccessLayer.Services;
using DataAccessLayer.Services.Models;
using DataAccess;
using FptSocialNetwork.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FptSocialNetwork.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly MyDbContext _dbContext;

        public ConversationsController(
            IConversationService conversationService,
            IHubContext<ChatHub> hubContext,
            MyDbContext dbContext)
        {
            _conversationService = conversationService;
            _hubContext = hubContext;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<List<ConversationListItemDto>>> GetConversations(
            [FromQuery] string? tab,
            [FromQuery] string? q)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var conversations = await _conversationService.GetConversationsAsync(userId, tab, q);
            return Ok(conversations);
        }

        [HttpGet("{conversationId:int}")]
        public async Task<ActionResult<ConversationDetailDto>> GetConversationDetail(int conversationId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var conversation = await _conversationService.GetConversationDetailAsync(conversationId, userId);
            if (conversation is null)
            {
                return NotFound();
            }

            return Ok(conversation);
        }

        [HttpPost]
        public async Task<ActionResult<ConversationDetailDto>> CreateConversation([FromBody] CreateConversationRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            if (request.MemberUserIds is null || request.MemberUserIds.Count == 0)
            {
                return BadRequest("MemberUserIds must not be empty.");
            }

            try
            {
                var created = await _conversationService.CreateConversationAsync(userId, request);
                await BroadcastConversationUpdatedAsync(created.Id);
                return CreatedAtAction(nameof(GetConversationDetail), new { conversationId = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{conversationId:int}/settings")]
        public async Task<ActionResult<ConversationDetailDto>> UpdateConversationSettings(int conversationId, [FromBody] UpdateConversationSettingsRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var updated = await _conversationService.UpdateConversationSettingsAsync(conversationId, userId, request.Name, request.AvatarUrl);
                await BroadcastConversationUpdatedAsync(conversationId);
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

        [HttpPost("{conversationId:int}/members")]
        public async Task<ActionResult<ConversationDetailDto>> AddMembers(int conversationId, [FromBody] AddConversationMembersRequest request)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var updated = await _conversationService.AddMembersAsync(conversationId, userId, request.MemberUserIds);
                await BroadcastConversationUpdatedAsync(conversationId);
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

        [HttpPost("{conversationId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int conversationId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var readAt = await _conversationService.MarkConversationAsReadAsync(conversationId, userId);
                await _hubContext.Clients
                    .Group(ChatHub.GetConversationGroup(conversationId))
                    .SendAsync("SeenUpdated", new
                    {
                        conversationId,
                        userId,
                        readAt
                    });
                await BroadcastConversationUpdatedAsync(conversationId);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("direct/{targetUserId:int}")]
        public async Task<ActionResult<ConversationDetailDto>> GetOrCreateDirectConversation(int targetUserId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var conversation = await _conversationService.GetOrCreateDirectConversationAsync(userId, targetUserId);
                return Ok(conversation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task BroadcastConversationUpdatedAsync(int conversationId)
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
                .SendAsync("ConversationUpdated", new { conversationId });
        }
    }
}
