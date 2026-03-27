using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FptSocialNetwork.Client.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        public IFormFile? AvatarImage { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
