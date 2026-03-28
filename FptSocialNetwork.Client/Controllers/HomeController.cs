using System.Diagnostics;
using DataAccessLayer.Services.Models;
using FptSocialNetwork.Client.Models;
using FptSocialNetwork.Client.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FptSocialNetwork.Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPostApiService _postApiService;
        private readonly IUserApiService _userApiService;
        private readonly ICloudinaryUploadService _cloudinaryUploadService;
        private readonly IConfiguration _configuration;

        public HomeController(
            ILogger<HomeController> logger,
            IPostApiService postApiService,
            IUserApiService userApiService,
            ICloudinaryUploadService cloudinaryUploadService,
            IConfiguration configuration)
        {
            _logger = logger;
            _postApiService = postApiService;
            _userApiService = userApiService;
            _cloudinaryUploadService = cloudinaryUploadService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

            var viewModel = new HomeFeedViewModel
            {
                ComposerPlaceholder = "Bạn đang nghĩ gì thế?",
                PostError = TempData["PostError"]?.ToString() ?? string.Empty,
                RealtimeToken = HttpContext.Session.GetString("AccessToken") ?? string.Empty,
                SocialHubUrl = BuildSocialHubUrl(),
                ViewedUserId = currentUserId,
                IsOwnProfile = true
            };
            if (await TryLoadProfileAsync(viewModel, currentUserId, persistToSession: true) is IActionResult unauthorizedProfileResult)
            {
                return unauthorizedProfileResult;
            }

            if (await TryLoadFollowersAsync(viewModel, currentUserId) is IActionResult unauthorizedFollowersResult)
            {
                return unauthorizedFollowersResult;
            }

            var feedResponse = await _postApiService.GetFeedAsync();
            if (IsUnauthorized(feedResponse.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (feedResponse.IsSuccess && feedResponse.Data is not null)
            {
                viewModel.Posts = feedResponse.Data;
            }
            else if (!string.IsNullOrWhiteSpace(feedResponse.ErrorMessage))
            {
                viewModel.PostError = feedResponse.ErrorMessage;
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("Home/Profile/{userId?}")]
        public async Task<IActionResult> Profile(int? userId = null)
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var viewModel = new HomeFeedViewModel
            {
                ComposerPlaceholder = "Bạn đang nghĩ gì?",
                PostError = TempData["PostError"]?.ToString() ?? string.Empty,
                ProfileError = TempData["ProfileError"]?.ToString() ?? string.Empty,
                RealtimeToken = HttpContext.Session.GetString("AccessToken") ?? string.Empty,
                SocialHubUrl = BuildSocialHubUrl()
            };

            var targetUserId = userId ?? (HttpContext.Session.GetInt32("UserId") ?? 0);
            if (targetUserId <= 0)
            {
                return RedirectToLoginAndClearSession();
            }

            var isOwnProfile = targetUserId == (HttpContext.Session.GetInt32("UserId") ?? 0);
            viewModel.IsOwnProfile = isOwnProfile;
            viewModel.ViewedUserId = targetUserId;

            if (await TryLoadProfileAsync(viewModel, targetUserId, persistToSession: isOwnProfile) is IActionResult unauthorizedProfileResult)
            {
                return unauthorizedProfileResult;
            }

            if (await TryLoadFollowersAsync(viewModel, targetUserId) is IActionResult unauthorizedFollowersResult)
            {
                return unauthorizedFollowersResult;
            }

            if (!isOwnProfile)
            {
                var followStatusResponse = await _userApiService.GetFollowStatusAsync(targetUserId);
                if (IsUnauthorized(followStatusResponse.StatusCode))
                {
                    return RedirectToLoginAndClearSession();
                }

                if (followStatusResponse.IsSuccess)
                {
                    viewModel.IsFollowingViewedUser = followStatusResponse.Data;
                }
            }

            var postsResponse = isOwnProfile
                ? await _postApiService.GetMyPostsAsync()
                : await _postApiService.GetFeedAsync();

            if (IsUnauthorized(postsResponse.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (postsResponse.IsSuccess && postsResponse.Data is not null)
            {
                viewModel.Posts = postsResponse.Data
                    .Where(post => post.UserId == targetUserId)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(postsResponse.ErrorMessage))
            {
                viewModel.PostError = postsResponse.ErrorMessage;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(CreatePostInputModel input)
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var target = ResolveTarget(input.Source);

            var hasContent = !string.IsNullOrWhiteSpace(input.Content);
            var hasImage = input.Image is not null && input.Image.Length > 0;
            if (!hasContent && !hasImage)
            {
                TempData["PostError"] = "Bạn cần nhập nội dung hoặc chọn ảnh để đăng bài.";
                return RedirectToAction(target);
            }

            var createResponse = await _postApiService.CreatePostAsync(input.Content, input.Image, input.PostStatusId);
            if (IsUnauthorized(createResponse.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!createResponse.IsSuccess)
            {
                TempData["PostError"] = createResponse.ErrorMessage ?? "Đăng bài thất bại.";
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReaction(long postId, string source = "index")
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var target = ResolveTarget(source);
            var response = await _postApiService.ToggleReactionAsync(postId);
            if (IsUnauthorized(response.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể thả cảm xúc lúc này.";
                if (IsAjaxRequest())
                {
                    return BadRequest(response.ErrorMessage ?? "Không thể thả cảm xúc lúc này.");
                }
            }

            if (IsAjaxRequest())
            {
                return Ok(response.Data);
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(long postId, string content, string source = "index")
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var target = ResolveTarget(source);
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["PostError"] = "Nội dung bình luận không được để trống.";
                if (IsAjaxRequest())
                {
                    return BadRequest("Nội dung bình luận không được để trống.");
                }
                return RedirectToAction(target);
            }

            var response = await _postApiService.AddCommentAsync(postId, content);
            if (IsUnauthorized(response.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể bình luận lúc này.";
                if (IsAjaxRequest())
                {
                    return BadRequest(response.ErrorMessage ?? "Không thể bình luận lúc này.");
                }
            }

            if (IsAjaxRequest())
            {
                return Ok(response.Data);
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SharePost(long postId, string source = "index")
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var target = ResolveTarget(source);
            var response = await _postApiService.SharePostAsync(postId);
            if (IsUnauthorized(response.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể chia sẻ bài viết lúc này.";
                if (IsAjaxRequest())
                {
                    return BadRequest(response.ErrorMessage ?? "Không thể chia sẻ bài viết lúc này.");
                }
            }

            if (IsAjaxRequest())
            {
                return Ok(response.Data);
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(long postId, string content, string source = "index")
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var target = ResolveTarget(source);
            var response = await _postApiService.UpdatePostAsync(postId, content);
            if (IsUnauthorized(response.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể chỉnh sửa bài viết lúc này.";
                if (IsAjaxRequest())
                {
                    return BadRequest(response.ErrorMessage ?? "Không thể chỉnh sửa bài viết lúc này.");
                }
            }

            if (IsAjaxRequest())
            {
                return Ok(response.Data);
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(long postId, string source = "index")
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var target = ResolveTarget(source);
            var response = await _postApiService.DeletePostAsync(postId);
            if (IsUnauthorized(response.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể xóa bài viết lúc này.";
                if (IsAjaxRequest())
                {
                    return BadRequest(response.ErrorMessage ?? "Không thể xóa bài viết lúc này.");
                }
            }

            if (IsAjaxRequest())
            {
                return Ok(response.Data);
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollow(int targetUserId, string source = "index")
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var target = ResolveTarget(source);
            if (targetUserId <= 0)
            {
                TempData["PostError"] = "Không xác định được người dùng cần theo dõi.";
                return RedirectToAction(target);
            }

            var response = await _userApiService.ToggleFollowAsync(targetUserId);
            if (IsUnauthorized(response.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể cập nhật theo dõi lúc này.";
                if (IsAjaxRequest())
                {
                    return BadRequest(response.ErrorMessage ?? "Không thể cập nhật theo dõi lúc này.");
                }
            }

            if (IsAjaxRequest())
            {
                return Ok(response.Data);
            }

            return RedirectToAction(target);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsersJson(string? q)
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new List<UserSearchItemDto>());
            }

            var response = await _userApiService.SearchUsersAsync(q);
            if (IsUnauthorized(response.StatusCode))
            {
                HttpContext.Session.Clear();
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (!response.IsSuccess)
            {
                return BadRequest(new { error = response.ErrorMessage ?? "Không thể tìm kiếm người dùng lúc này." });
            }

            return Ok(response.Data ?? new List<UserSearchItemDto>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileInputModel input)
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            var updateResponse = await _userApiService.UpdateMyProfileAsync(new UpdateMyProfileRequest
            {
                FullName = input.FullName,
                Bio = input.Bio,
                City = input.City,
                School = input.School
            });

            if (IsUnauthorized(updateResponse.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!updateResponse.IsSuccess || updateResponse.Data is null)
            {
                TempData["ProfileError"] = updateResponse.ErrorMessage ?? "Cập nhật trang cá nhân thất bại.";
                return RedirectToAction(nameof(Profile));
            }

            HttpContext.Session.SetString("UserName", updateResponse.Data.FullName);
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvatar(UpdateAvatarInputModel input)
        {
            var redirectToLogin = EnsureAuthenticated();
            if (redirectToLogin is not null)
            {
                return redirectToLogin;
            }

            if (input.AvatarImage is null || input.AvatarImage.Length <= 0)
            {
                TempData["ProfileError"] = "Bạn chưa chọn ảnh đại diện.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                var avatarUrl = await _cloudinaryUploadService.UploadImageAsync(input.AvatarImage);
                var updateResponse = await _userApiService.UpdateMyProfileAsync(new UpdateMyProfileRequest
                {
                    AvatarUrl = avatarUrl
                });

                if (IsUnauthorized(updateResponse.StatusCode))
                {
                    return RedirectToLoginAndClearSession();
                }

                if (!updateResponse.IsSuccess)
                {
                    TempData["ProfileError"] = updateResponse.ErrorMessage ?? "Đổi ảnh đại diện thất bại.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ProfileError"] = ex.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<IActionResult?> TryLoadProfileAsync(HomeFeedViewModel viewModel, int targetUserId, bool persistToSession)
        {
            viewModel.CurrentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

            var profileResponse = targetUserId == viewModel.CurrentUserId
                ? await _userApiService.GetMyProfileAsync()
                : await _userApiService.GetProfileAsync(targetUserId);
            if (IsUnauthorized(profileResponse.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!profileResponse.IsSuccess || profileResponse.Data is null)
            {
                if (string.IsNullOrWhiteSpace(viewModel.ProfileError))
                {
                    viewModel.ProfileError = profileResponse.ErrorMessage ?? string.Empty;
                }
                return null;
            }

            viewModel.CurrentUserName = profileResponse.Data.FullName;
            viewModel.CurrentUserAvatarUrl = profileResponse.Data.AvatarUrl;
            viewModel.CurrentUserId = profileResponse.Data.UserId;
            viewModel.Bio = profileResponse.Data.Bio;
            viewModel.City = profileResponse.Data.City;
            viewModel.School = profileResponse.Data.School;
            viewModel.FollowerCount = profileResponse.Data.FollowerCount;
            viewModel.FollowingCount = profileResponse.Data.FollowingCount;

            if (persistToSession)
            {
                HttpContext.Session.SetString("UserName", profileResponse.Data.FullName);
                HttpContext.Session.SetString("UserAvatarUrl", profileResponse.Data.AvatarUrl ?? string.Empty);
            }

            return null;
        }

        private async Task<IActionResult?> TryLoadFollowersAsync(HomeFeedViewModel viewModel, int targetUserId)
        {
            var followersResponse = targetUserId == (HttpContext.Session.GetInt32("UserId") ?? 0)
                ? await _userApiService.GetMyFollowersAsync(40)
                : await _userApiService.GetFollowersAsync(targetUserId, 40);
            if (IsUnauthorized(followersResponse.StatusCode))
            {
                return RedirectToLoginAndClearSession();
            }

            if (!followersResponse.IsSuccess || followersResponse.Data is null)
            {
                if (string.IsNullOrWhiteSpace(viewModel.ProfileError))
                {
                    viewModel.ProfileError = followersResponse.ErrorMessage ?? string.Empty;
                }
                return null;
            }

            viewModel.FollowingUsers = followersResponse.Data
                .Select(user => new FeedUserViewModel
                {
                    UserId = user.UserId,
                    DisplayName = string.IsNullOrWhiteSpace(user.FullName) ? "Người dùng" : user.FullName,
                    AvatarUrl = user.AvatarUrl ?? string.Empty
                })
                .ToList();
            return null;
        }

        private IActionResult? EnsureAuthenticated()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (userId is null || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account");
            }

            return null;
        }

        private IActionResult RedirectToLoginAndClearSession()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        private static bool IsUnauthorized(int statusCode)
        {
            return statusCode == StatusCodes.Status401Unauthorized;
        }

        private string BuildSocialHubUrl()
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? string.Empty;
            if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri))
            {
                return new Uri(baseUri, "/hubs/social").ToString();
            }

            return "/hubs/social";
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveTarget(string? source)
        {
            return string.Equals(source, "profile", StringComparison.OrdinalIgnoreCase)
                ? nameof(Profile)
                : nameof(Index);
        }
    }
}
