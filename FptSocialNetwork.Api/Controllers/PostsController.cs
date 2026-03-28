using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using DataAccessLayer.Services;
using DataAccessLayer.Services.Models;
using FptSocialNetwork.Api.Hubs;
using FptSocialNetwork.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FptSocialNetwork.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ICloudinaryUploadService _cloudinaryUploadService;
        private readonly IHubContext<SocialHub> _socialHubContext;

        public PostsController(
            IPostService postService,
            ICloudinaryUploadService cloudinaryUploadService,
            IHubContext<SocialHub> socialHubContext)
        {
            _postService = postService;
            _cloudinaryUploadService = cloudinaryUploadService;
            _socialHubContext = socialHubContext;
        }

        [HttpGet("{postId:long}")]
        public async Task<ActionResult<PostDto>> GetById(long postId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var post = await _postService.GetPostByIdAsync(postId, userId.Value);
            if (post is null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpGet]
        public async Task<ActionResult<List<PostDto>>> GetFeed(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var posts = await _postService.GetFeedAsync(userId.Value, page, pageSize);
            return Ok(posts);
        }

        [HttpGet("me")]
        public async Task<ActionResult<List<PostDto>>> GetMyPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var posts = await _postService.GetPostsByUserAsync(userId.Value, userId.Value, page, pageSize);
            return Ok(posts);
        }

        [HttpPost]
        [RequestSizeLimit(25_000_000)]
        public async Task<ActionResult<PostDto>> CreatePost([FromForm] CreatePostFormRequest request, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                string? mediaUrl = null;
                if (request.Image is not null && request.Image.Length > 0)
                {
                    mediaUrl = await _cloudinaryUploadService.UploadImageAsync(request.Image, cancellationToken);
                }

                var created = await _postService.CreatePostAsync(new CreatePostRequest
                {
                    UserId = userId.Value,
                    Content = request.Content ?? string.Empty,
                    PostStatusId = request.PostStatusId,
                    MediaUrl = mediaUrl
                });

                return CreatedAtAction(nameof(GetById), new { postId = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{postId:long}/react")]
        public async Task<ActionResult<ToggleReactionResultDto>> ToggleReaction(long postId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var post = await _postService.GetPostByIdAsync(postId, userId.Value);
                if (post is null)
                {
                    return NotFound();
                }

                var result = await _postService.ToggleReactionAsync(postId, userId.Value);
                await NotifyReactionAsync(post, result, userId.Value);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{postId:long}/comments")]
        public async Task<ActionResult<PostCommentDto>> AddComment(long postId, [FromBody] AddCommentRequest? request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var post = await _postService.GetPostByIdAsync(postId, userId.Value);
                if (post is null)
                {
                    return NotFound();
                }

                var result = await _postService.AddCommentAsync(postId, userId.Value, request?.Content ?? string.Empty);
                var updatedPost = await _postService.GetPostByIdAsync(postId, userId.Value);
                await NotifyCommentAsync(post, result, updatedPost?.CommentCount ?? post.CommentCount + 1, userId.Value);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{postId:long}/share")]
        public async Task<ActionResult<PostDto>> SharePost(long postId, [FromBody] SharePostRequest? request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var sourcePost = await _postService.GetPostByIdAsync(postId, userId.Value);
                if (sourcePost is null)
                {
                    return NotFound();
                }

                var sharedPost = await _postService.SharePostAsync(
                    postId,
                    userId.Value,
                    request?.Content,
                    request?.PostStatusId);
                await NotifyShareAsync(sourcePost, sharedPost, userId.Value);
                return Ok(sharedPost);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{postId:long}")]
        public async Task<ActionResult<PostDto>> UpdatePost(long postId, [FromBody] UpdatePostRequest? request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var updatedPost = await _postService.UpdatePostAsync(postId, userId.Value, request ?? new UpdatePostRequest());
                await _socialHubContext.Clients.Group(SocialHub.FeedGroup)
                    .SendAsync("PostUpdated", new
                    {
                        post = updatedPost
                    });
                return Ok(updatedPost);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{postId:long}")]
        public async Task<ActionResult<DeletePostResultDto>> DeletePost(long postId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                await _postService.DeletePostAsync(postId, userId.Value);
                await _socialHubContext.Clients.Group(SocialHub.FeedGroup)
                    .SendAsync("PostDeleted", new
                    {
                        postId
                    });
                return Ok(new DeletePostResultDto { PostId = postId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task NotifyReactionAsync(PostDto post, ToggleReactionResultDto result, int actorUserId)
        {
            await _socialHubContext.Clients.Group(SocialHub.FeedGroup)
                .SendAsync("PostReactionUpdated", new
                {
                    postId = result.PostId,
                    likeCount = result.LikeCount
                });

            if (post.UserId == actorUserId || !result.IsLiked)
            {
                return;
            }

            await SendUserNotificationAsync(
                post.UserId,
                "like",
                $"{GetCurrentUserDisplayName()} đã thích bài viết của bạn.",
                post.Id);
        }

        private async Task NotifyCommentAsync(PostDto post, PostCommentDto comment, int commentCount, int actorUserId)
        {
            await _socialHubContext.Clients.Group(SocialHub.FeedGroup)
                .SendAsync("PostCommentAdded", new
                {
                    postId = comment.PostId,
                    commentCount,
                    comment = comment
                });

            if (post.UserId == actorUserId)
            {
                return;
            }

            await SendUserNotificationAsync(
                post.UserId,
                "comment",
                $"{GetCurrentUserDisplayName()} đã bình luận về bài viết của bạn.",
                post.Id);
        }

        private async Task NotifyShareAsync(PostDto sourcePost, PostDto sharedPost, int actorUserId)
        {
            await _socialHubContext.Clients.Group(SocialHub.FeedGroup)
                .SendAsync("PostShared", new
                {
                    sourcePostId = sourcePost.Id,
                    sharedPostId = sharedPost.Id,
                    actorUserId,
                    actorName = GetCurrentUserDisplayName(),
                    createdAt = DateTime.UtcNow,
                    post = sharedPost
                });

            if (sourcePost.UserId == actorUserId)
            {
                return;
            }

            await SendUserNotificationAsync(
                sourcePost.UserId,
                "share",
                $"{GetCurrentUserDisplayName()} đã chia sẻ bài viết của bạn.",
                sourcePost.Id);
        }

        private async Task SendUserNotificationAsync(int targetUserId, string type, string message, long postId)
        {
            await _socialHubContext.Clients.Group(SocialHub.GetUserGroup(targetUserId))
                .SendAsync("NotificationReceived", new
                {
                    type,
                    message,
                    postId,
                    actorName = GetCurrentUserDisplayName(),
                    createdAt = DateTime.UtcNow
                });
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }

        private string GetCurrentUserDisplayName()
        {
            return User.FindFirstValue(ClaimTypes.GivenName)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Name)
                ?? User.Identity?.Name
                ?? "Ai đó";
        }
    }

    public class CreatePostFormRequest
    {
        public string? Content { get; set; }
        public int? PostStatusId { get; set; }
        public IFormFile? Image { get; set; }
    }
}
