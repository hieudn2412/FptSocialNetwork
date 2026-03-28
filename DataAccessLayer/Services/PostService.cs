using DataAccess;
using DataAccessLayer.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Services
{
    public class PostService : IPostService
    {
        private readonly MyDbContext _context;

        public PostService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<PostDto> CreatePostAsync(CreatePostRequest request)
        {
            var hasContent = !string.IsNullOrWhiteSpace(request.Content);
            var hasMedia = !string.IsNullOrWhiteSpace(request.MediaUrl);

            if (!hasContent && !hasMedia)
            {
                throw new InvalidOperationException("Post content or image is required.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
            {
                throw new InvalidOperationException("User not found.");
            }

            var postStatusId = request.PostStatusId ?? await ResolveDefaultStatusIdAsync();
            var statusExists = await _context.PostStatuses.AnyAsync(s => s.Id == postStatusId);
            if (!statusExists)
            {
                throw new InvalidOperationException("Post status is invalid.");
            }

            var post = new Post
            {
                UserId = request.UserId,
                PostStatusId = postStatusId,
                Content = request.Content?.Trim() ?? string.Empty,
                MediaUrl = request.MediaUrl?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();

            return (await GetPostByIdAsync(post.Id, request.UserId))!;
        }

        public async Task<PostDto?> GetPostByIdAsync(long postId, int currentUserId)
        {
            return await QueryPosts(currentUserId)
                .Where(p => p.Id == postId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PostDto>> GetFeedAsync(int currentUserId, int page, int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            var skip = (page - 1) * pageSize;

            return await QueryPosts(currentUserId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<PostDto>> GetPostsByUserAsync(int userId, int currentUserId, int page, int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            var skip = (page - 1) * pageSize;

            return await QueryPosts(currentUserId)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<ToggleReactionResultDto> ToggleReactionAsync(long postId, int userId)
        {
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId && !p.IsDeleted);
            if (!postExists)
            {
                throw new InvalidOperationException("Post not found.");
            }

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            var isLiked = false;
            if (existingLike is null)
            {
                await _context.Likes.AddAsync(new Like
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                isLiked = true;
            }
            else
            {
                _context.Likes.Remove(existingLike);
            }

            await _context.SaveChangesAsync();

            var likeCount = await _context.Likes.CountAsync(l => l.PostId == postId);
            return new ToggleReactionResultDto
            {
                PostId = postId,
                IsLiked = isLiked,
                LikeCount = likeCount
            };
        }

        public async Task<PostCommentDto> AddCommentAsync(long postId, int userId, string content)
        {
            var normalizedContent = content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedContent))
            {
                throw new InvalidOperationException("Comment content is required.");
            }

            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId && !p.IsDeleted);
            if (!postExists)
            {
                throw new InvalidOperationException("Post not found.");
            }

            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                Content = normalizedContent,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            var result = await _context.Comments
                .Where(c => c.Id == comment.Id)
                .Select(c => new PostCommentDto
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    UserId = c.UserId,
                    AuthorName = c.User.FullName,
                    AuthorAvatarUrl = c.User.AvatarUrl,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            return result!;
        }

        public async Task<PostDto> SharePostAsync(long sourcePostId, int userId, string? content, int? postStatusId)
        {
            var sourcePost = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == sourcePostId && !p.IsDeleted);

            if (sourcePost is null)
            {
                throw new InvalidOperationException("Post not found.");
            }

            var normalizedShareContent = content?.Trim() ?? string.Empty;
            var combinedContent = string.IsNullOrWhiteSpace(normalizedShareContent)
                ? $"Đã chia sẻ bài viết của {sourcePost.User.FullName}."
                : normalizedShareContent;

            var statusId = postStatusId ?? await ResolveDefaultStatusIdAsync();
            var statusExists = await _context.PostStatuses.AnyAsync(s => s.Id == statusId);
            if (!statusExists)
            {
                throw new InvalidOperationException("Post status is invalid.");
            }

            var sharedPost = new Post
            {
                UserId = userId,
                PostStatusId = statusId,
                Content = combinedContent,
                // Shared posts render the source content block separately, so we avoid
                // duplicating the source media on the outer shared post.
                MediaUrl = string.Empty,
                SharedFromPostId = sourcePostId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _context.Posts.AddAsync(sharedPost);
            await _context.SaveChangesAsync();

            return (await GetPostByIdAsync(sharedPost.Id, userId))!;
        }

        public async Task<PostDto> UpdatePostAsync(long postId, int userId, UpdatePostRequest request)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is null)
            {
                throw new InvalidOperationException("Post not found.");
            }

            if (post.UserId != userId)
            {
                throw new InvalidOperationException("Bạn không thể chỉnh sửa bài viết này.");
            }

            var normalizedContent = request.Content?.Trim() ?? string.Empty;
            var hasExternalBody = !string.IsNullOrWhiteSpace(post.MediaUrl) || post.SharedFromPostId.HasValue;
            if (string.IsNullOrWhiteSpace(normalizedContent) && !hasExternalBody)
            {
                throw new InvalidOperationException("Nội dung bài viết không được để trống.");
            }

            if (request.PostStatusId.HasValue)
            {
                var statusExists = await _context.PostStatuses.AnyAsync(s => s.Id == request.PostStatusId.Value);
                if (!statusExists)
                {
                    throw new InvalidOperationException("Post status is invalid.");
                }

                post.PostStatusId = request.PostStatusId.Value;
            }

            post.Content = normalizedContent;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (await GetPostByIdAsync(post.Id, userId))!;
        }

        public async Task DeletePostAsync(long postId, int userId)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is null)
            {
                throw new InvalidOperationException("Post not found.");
            }

            if (post.UserId != userId)
            {
                throw new InvalidOperationException("Bạn không thể xóa bài viết này.");
            }

            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private static void NormalizePaging(ref int page, ref int pageSize)
        {
            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }
        }

        private async Task<int> ResolveDefaultStatusIdAsync()
        {
            var existing = await _context.PostStatuses
                .Where(s => s.IsActive && s.Name.ToLower() == "public")
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (existing > 0)
            {
                return existing;
            }

            var status = new PostStatus
            {
                Name = "public",
                Description = "Visible to everyone",
                IsActive = true
            };

            await _context.PostStatuses.AddAsync(status);
            await _context.SaveChangesAsync();
            return status.Id;
        }

        private IQueryable<PostDto> QueryPosts(int currentUserId)
        {
            return _context.Posts
                .Where(p => !p.IsDeleted)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    AuthorName = p.User.FullName,
                    AuthorAvatarUrl = p.User.AvatarUrl,
                    Content = p.Content,
                    MediaUrl = p.MediaUrl,
                    PostStatusId = p.PostStatusId,
                    PostStatusName = p.PostStatus.Name,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    LikeCount = p.Likes.Count(),
                    CommentCount = p.Comments.Count(c => !c.IsDeleted),
                    IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
                    IsAuthorFollowedByCurrentUser = _context.Follows.Any(f => f.FollowerId == currentUserId && f.FollowingId == p.UserId),
                    SourcePostId = p.SharedFromPostId,
                    SourceUserId = p.SharedFromPost != null ? p.SharedFromPost.UserId : null,
                    SourceAuthorName = p.SharedFromPost != null ? p.SharedFromPost.User.FullName : string.Empty,
                    SourceAuthorAvatarUrl = p.SharedFromPost != null ? p.SharedFromPost.User.AvatarUrl : string.Empty,
                    SourceContent = p.SharedFromPost != null ? p.SharedFromPost.Content : string.Empty,
                    SourceMediaUrl = p.SharedFromPost != null ? p.SharedFromPost.MediaUrl : string.Empty,
                    SourceCreatedAt = p.SharedFromPost != null ? p.SharedFromPost.CreatedAt : null,
                    SourcePostDeleted = p.SharedFromPost != null && p.SharedFromPost.IsDeleted,
                    RecentComments = p.Comments
                        .Where(c => !c.IsDeleted)
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(5)
                        .Select(c => new PostCommentDto
                        {
                            Id = c.Id,
                            PostId = c.PostId,
                            UserId = c.UserId,
                            AuthorName = c.User.FullName,
                            AuthorAvatarUrl = c.User.AvatarUrl,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt
                        })
                        .ToList()
                });
        }
    }
}
