using System.Security.Claims;
using DataAccessLayer.Services;
using DataAccessLayer.Services.Models;
using FptSocialNetwork.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FptSocialNetwork.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ICloudinaryUploadService _cloudinaryUploadService;

        public PostsController(
            IPostService postService,
            ICloudinaryUploadService cloudinaryUploadService)
        {
            _postService = postService;
            _cloudinaryUploadService = cloudinaryUploadService;
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
                var result = await _postService.ToggleReactionAsync(postId, userId.Value);
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
                var result = await _postService.AddCommentAsync(postId, userId.Value, request?.Content ?? string.Empty);
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
                var sharedPost = await _postService.SharePostAsync(
                    postId,
                    userId.Value,
                    request?.Content,
                    request?.PostStatusId);
                return Ok(sharedPost);
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
    }

    public class CreatePostFormRequest
    {
        public string? Content { get; set; }
        public int? PostStatusId { get; set; }
        public IFormFile? Image { get; set; }
    }
}
