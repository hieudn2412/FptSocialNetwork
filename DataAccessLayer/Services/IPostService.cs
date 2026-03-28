using DataAccessLayer.Services.Models;

namespace DataAccessLayer.Services
{
    public interface IPostService
    {
        Task<PostDto> CreatePostAsync(CreatePostRequest request);
        Task<PostDto?> GetPostByIdAsync(long postId, int currentUserId);
        Task<List<PostDto>> GetFeedAsync(int currentUserId, int page, int pageSize);
        Task<List<PostDto>> GetPostsByUserAsync(int userId, int currentUserId, int page, int pageSize);
        Task<ToggleReactionResultDto> ToggleReactionAsync(long postId, int userId);
        Task<PostCommentDto> AddCommentAsync(long postId, int userId, string content);
        Task<PostDto> SharePostAsync(long sourcePostId, int userId, string? content, int? postStatusId);
        Task<PostDto> UpdatePostAsync(long postId, int userId, UpdatePostRequest request);
        Task DeletePostAsync(long postId, int userId);
    }
}
