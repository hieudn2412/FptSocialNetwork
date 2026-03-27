using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DataAccessLayer.Services.Models;
using Microsoft.AspNetCore.Http;

namespace FptSocialNetwork.Client.Services
{
    public class PostApiService : IPostApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PostApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<List<PostDto>>> GetFeedAsync(int page = 1, int pageSize = 20)
        {
            return await SendAsync<List<PostDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/posts?page={page}&pageSize={pageSize}"));
        }

        public async Task<ApiResponse<List<PostDto>>> GetMyPostsAsync(int page = 1, int pageSize = 20)
        {
            return await SendAsync<List<PostDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/posts/me?page={page}&pageSize={pageSize}"));
        }

        public async Task<ApiResponse<PostDto>> CreatePostAsync(string content, IFormFile? image, int? postStatusId = null)
        {
            try
            {
                using var form = new MultipartFormDataContent();

                if (!string.IsNullOrWhiteSpace(content))
                {
                    form.Add(new StringContent(content), "Content");
                }

                if (postStatusId.HasValue)
                {
                    form.Add(new StringContent(postStatusId.Value.ToString()), "PostStatusId");
                }

                Stream? imageStream = null;
                if (image is not null && image.Length > 0)
                {
                    imageStream = image.OpenReadStream();
                    var imageContent = new StreamContent(imageStream);
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType ?? "application/octet-stream");
                    form.Add(imageContent, "Image", image.FileName);
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, "api/posts")
                {
                    Content = form
                };

                AttachBearerToken(request);
                var response = await _httpClient.SendAsync(request);
                imageStream?.Dispose();
                return await BuildResponse<PostDto>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<PostDto>
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = $"Network error while calling API: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<ToggleReactionResultDto>> ToggleReactionAsync(long postId)
        {
            return await SendAsync<ToggleReactionResultDto>(new HttpRequestMessage(HttpMethod.Post, $"api/posts/{postId}/react"));
        }

        public async Task<ApiResponse<PostCommentDto>> AddCommentAsync(long postId, string content)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/posts/{postId}/comments")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new AddCommentRequest { Content = content }),
                    Encoding.UTF8,
                    "application/json")
            };

            return await SendAsync<PostCommentDto>(request);
        }

        public async Task<ApiResponse<PostDto>> SharePostAsync(long postId, string? content = null, int? postStatusId = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/posts/{postId}/share")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SharePostRequest
                    {
                        Content = content,
                        PostStatusId = postStatusId
                    }),
                    Encoding.UTF8,
                    "application/json")
            };

            return await SendAsync<PostDto>(request);
        }

        private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request)
        {
            try
            {
                using (request)
                {
                    AttachBearerToken(request);
                    var response = await _httpClient.SendAsync(request);
                    return await BuildResponse<T>(response);
                }
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = $"Network error while calling API: {ex.Message}"
                };
            }
        }

        private void AttachBearerToken(HttpRequestMessage request)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private static async Task<ApiResponse<T>> BuildResponse<T>(HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = $"API Error: {response.StatusCode}. Response: {responseText}"
                };
            }

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return new ApiResponse<T>
                {
                    IsSuccess = true,
                    StatusCode = statusCode,
                    Data = default
                };
            }

            try
            {
                var data = JsonSerializer.Deserialize<T>(responseText, JsonOptions);
                return new ApiResponse<T>
                {
                    IsSuccess = true,
                    StatusCode = statusCode,
                    Data = data
                };
            }
            catch (JsonException ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = $"Deserialize failed: {ex.Message}. Response: {responseText}"
                };
            }
        }
    }
}
