using DataAccessLayer.Services.Models;
using Microsoft.AspNetCore.Http;

namespace FptSocialNetwork.Client.Services
{
    public interface IPostApiService
    {
        Task<ApiResponse<List<PostDto>>> GetFeedAsync(int page = 1, int pageSize = 20);
        Task<ApiResponse<List<PostDto>>> GetMyPostsAsync(int page = 1, int pageSize = 20);
        Task<ApiResponse<PostDto>> CreatePostAsync(string content, IFormFile? image, int? postStatusId = null);
        Task<ApiResponse<ToggleReactionResultDto>> ToggleReactionAsync(long postId);
        Task<ApiResponse<PostCommentDto>> AddCommentAsync(long postId, string content);
        Task<ApiResponse<PostDto>> SharePostAsync(long postId, string? content = null, int? postStatusId = null);
        Task<ApiResponse<PostDto>> UpdatePostAsync(long postId, string content, int? postStatusId = null);
        Task<ApiResponse<DeletePostResultDto>> DeletePostAsync(long postId);
    }
}
