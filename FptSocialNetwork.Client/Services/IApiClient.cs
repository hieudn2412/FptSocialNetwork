namespace FptSocialNetwork.Client.Services
{
    public interface IApiClient
    {
        Task<ApiResponse<T>> GetAsync<T>(string endpoint);
        Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload);
        Task<ApiResponse<TResponse>> PostAsync<TResponse>(string endpoint);
    }
}
