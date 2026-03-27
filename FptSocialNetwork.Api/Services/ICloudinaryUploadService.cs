using Microsoft.AspNetCore.Http;

namespace FptSocialNetwork.Api.Services
{
    public interface ICloudinaryUploadService
    {
        Task<string> UploadImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default);
    }
}
