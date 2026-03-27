using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace FptSocialNetwork.Api.Services
{
    public class CloudinaryUploadService : ICloudinaryUploadService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _folder;

        public CloudinaryUploadService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"] ?? string.Empty;
            var apiKey = configuration["Cloudinary:ApiKey"] ?? string.Empty;
            var apiSecret = configuration["Cloudinary:ApiSecret"] ?? string.Empty;
            _folder = configuration["Cloudinary:PostsFolder"] ?? "social-network/posts";

            if (string.IsNullOrWhiteSpace(cloudName) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException("Cloudinary settings are missing.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile, CancellationToken cancellationToken = default)
        {
            if (imageFile is null || imageFile.Length == 0)
            {
                throw new InvalidOperationException("Image file is required.");
            }

            await using var stream = imageFile.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(imageFile.FileName, stream),
                Folder = _folder,
                PublicId = $"post_{Guid.NewGuid():N}",
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (uploadResult.Error is not null)
            {
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                throw new InvalidOperationException("Cloudinary response missing secure_url.");
            }

            return secureUrl;
        }
    }
}
