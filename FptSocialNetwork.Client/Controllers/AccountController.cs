using DataAccessLayer.Services.Models;
using FptSocialNetwork.Client.Models;
using FptSocialNetwork.Client.Services;
using Microsoft.AspNetCore.Mvc;

namespace FptSocialNetwork.Client.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthApiService _authApiService;
        private readonly ICloudinaryUploadService _cloudinaryUploadService;
        private readonly IConfiguration _configuration;

        public AccountController(
            IAuthApiService authApiService,
            ICloudinaryUploadService cloudinaryUploadService,
            IConfiguration configuration)
        {
            _authApiService = authApiService;
            _cloudinaryUploadService = cloudinaryUploadService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("AccessToken")))
            {
                return RedirectToAction("Index", "Conversation");
            }

            ViewBag.GoogleClientId = _configuration["GoogleAuth:ClientId"] ?? string.Empty;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authApiService.LoginAsync(new LoginRequest
            {
                Email = model.Email,
                Password = model.Password
            });

            if (!result.IsSuccess || result.Data is null)
            {
                model.ErrorMessage = result.ErrorMessage ?? "Đăng nhập thất bại.";
                return View(model);
            }

            HttpContext.Session.SetString("AccessToken", result.Data.Token);
            HttpContext.Session.SetInt32("UserId", result.Data.User.UserId);
            HttpContext.Session.SetString("UserName", result.Data.User.FullName);
            return RedirectToAction("Index", "Conversation");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleLogin(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                var model = new LoginViewModel { ErrorMessage = "Không nhận được token từ Google." };
                ViewBag.GoogleClientId = _configuration["GoogleAuth:ClientId"] ?? string.Empty;
                return View("Login", model);
            }

            var result = await _authApiService.GoogleLoginAsync(new GoogleLoginRequest
            {
                IdToken = idToken
            });

            if (!result.IsSuccess || result.Data is null)
            {
                var model = new LoginViewModel
                {
                    ErrorMessage = result.ErrorMessage ?? "Đăng nhập Google thất bại."
                };
                ViewBag.GoogleClientId = _configuration["GoogleAuth:ClientId"] ?? string.Empty;
                return View("Login", model);
            }

            HttpContext.Session.SetString("AccessToken", result.Data.Token);
            HttpContext.Session.SetInt32("UserId", result.Data.User.UserId);
            HttpContext.Session.SetString("UserName", result.Data.User.FullName);
            return RedirectToAction("Index", "Conversation");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? avatarUrl = null;
            if (model.AvatarImage is not null && model.AvatarImage.Length > 0)
            {
                try
                {
                    avatarUrl = await _cloudinaryUploadService.UploadImageAsync(model.AvatarImage);
                }
                catch (InvalidOperationException ex)
                {
                    model.ErrorMessage = ex.Message;
                    return View(model);
                }
            }

            var result = await _authApiService.RegisterAsync(new RegisterRequest
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                AvatarUrl = avatarUrl
            });

            if (!result.IsSuccess || result.Data is null)
            {
                model.ErrorMessage = result.ErrorMessage ?? "Đăng ký thất bại.";
                return View(model);
            }

            HttpContext.Session.SetString("AccessToken", result.Data.Token);
            HttpContext.Session.SetInt32("UserId", result.Data.User.UserId);
            HttpContext.Session.SetString("UserName", result.Data.User.FullName);
            return RedirectToAction("Index", "Conversation");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }
    }
}
