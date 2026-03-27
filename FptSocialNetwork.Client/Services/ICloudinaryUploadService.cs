using Microsoft.AspNetCore.Http;

namespace FptSocialNetwork.Client.Services
{
    public interface ICloudinaryUploadService
    {
        Task<string> UploadImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    }
}
