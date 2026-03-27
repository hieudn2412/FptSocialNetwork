using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace FptSocialNetwork.Client.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                AttachBearerToken(request);
                var response = await _httpClient.SendAsync(request);
                return await BuildResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                return BuildNetworkError<T>(ex);
            }
        }

        public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = content
                };
                AttachBearerToken(requestMessage);
                var response = await _httpClient.SendAsync(requestMessage);
                return await BuildResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return BuildNetworkError<TResponse>(ex);
            }
        }

        public async Task<ApiResponse<TResponse>> PostAsync<TResponse>(string endpoint)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                AttachBearerToken(request);
                var response = await _httpClient.SendAsync(request);
                return await BuildResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return BuildNetworkError<TResponse>(ex);
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

        private static ApiResponse<T> BuildNetworkError<T>(HttpRequestException ex)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = $"Network error while calling API: {ex.Message}"
            };
        }

        private static async Task<ApiResponse<T>> BuildResponse<T>(HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            var responseText = await response.Content.ReadAsStringAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType;

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = $"API Error: {response.StatusCode} - {response.ReasonPhrase}. Response: {responseText}"
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

            if (contentType is null || !contentType.Contains("json"))
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = $"Unexpected content type: {contentType ?? "unknown"}. Response: {responseText}"
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
