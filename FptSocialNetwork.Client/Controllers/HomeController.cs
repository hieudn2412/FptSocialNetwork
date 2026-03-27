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

        public HomeController(
            ILogger<HomeController> logger,
            IPostApiService postApiService,
            IUserApiService userApiService,
            ICloudinaryUploadService cloudinaryUploadService)
        {
            _logger = logger;
            _postApiService = postApiService;
            _userApiService = userApiService;
            _cloudinaryUploadService = cloudinaryUploadService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeFeedViewModel
            {
                ComposerPlaceholder = "Bạn đang nghĩ gì thế?",
                PostError = TempData["PostError"]?.ToString() ?? string.Empty
            };

            await LoadProfileAsync(viewModel);
            await LoadFollowersAsync(viewModel);

            var feedResponse = await _postApiService.GetFeedAsync();
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

        public async Task<IActionResult> Profile()
        {
            var viewModel = new HomeFeedViewModel
            {
                ComposerPlaceholder = "Bạn đang nghĩ gì?",
                PostError = TempData["PostError"]?.ToString() ?? string.Empty,
                ProfileError = TempData["ProfileError"]?.ToString() ?? string.Empty
            };

            await LoadProfileAsync(viewModel);
            await LoadFollowersAsync(viewModel);

            var myPostsResponse = await _postApiService.GetMyPostsAsync();
            if (myPostsResponse.IsSuccess && myPostsResponse.Data is not null)
            {
                viewModel.Posts = myPostsResponse.Data;
            }
            else if (!string.IsNullOrWhiteSpace(myPostsResponse.ErrorMessage))
            {
                viewModel.PostError = myPostsResponse.ErrorMessage;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(CreatePostInputModel input)
        {
            var target = ResolveTarget(input.Source);

            var hasContent = !string.IsNullOrWhiteSpace(input.Content);
            var hasImage = input.Image is not null && input.Image.Length > 0;
            if (!hasContent && !hasImage)
            {
                TempData["PostError"] = "Bạn cần nhập nội dung hoặc chọn ảnh để đăng bài.";
                return RedirectToAction(target);
            }

            var createResponse = await _postApiService.CreatePostAsync(input.Content, input.Image, input.PostStatusId);
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
            var target = ResolveTarget(source);
            var response = await _postApiService.ToggleReactionAsync(postId);
            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể thả cảm xúc lúc này.";
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(long postId, string content, string source = "index")
        {
            var target = ResolveTarget(source);
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["PostError"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction(target);
            }

            var response = await _postApiService.AddCommentAsync(postId, content);
            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể bình luận lúc này.";
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SharePost(long postId, string source = "index")
        {
            var target = ResolveTarget(source);
            var response = await _postApiService.SharePostAsync(postId);
            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể chia sẻ bài viết lúc này.";
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollow(int targetUserId, string source = "index")
        {
            var target = ResolveTarget(source);
            if (targetUserId <= 0)
            {
                TempData["PostError"] = "Không xác định được người dùng cần theo dõi.";
                return RedirectToAction(target);
            }

            var response = await _userApiService.ToggleFollowAsync(targetUserId);
            if (!response.IsSuccess)
            {
                TempData["PostError"] = response.ErrorMessage ?? "Không thể cập nhật theo dõi lúc này.";
            }

            return RedirectToAction(target);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileInputModel input)
        {
            var updateResponse = await _userApiService.UpdateMyProfileAsync(new UpdateMyProfileRequest
            {
                FullName = input.FullName,
                Bio = input.Bio,
                City = input.City,
                School = input.School
            });

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

        private async Task LoadProfileAsync(HomeFeedViewModel viewModel)
        {
            viewModel.CurrentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

            var profileResponse = await _userApiService.GetMyProfileAsync();
            if (!profileResponse.IsSuccess || profileResponse.Data is null)
            {
                if (string.IsNullOrWhiteSpace(viewModel.ProfileError))
                {
                    viewModel.ProfileError = profileResponse.ErrorMessage ?? string.Empty;
                }
                return;
            }

            viewModel.CurrentUserName = profileResponse.Data.FullName;
            viewModel.CurrentUserAvatarUrl = profileResponse.Data.AvatarUrl;
            viewModel.CurrentUserId = profileResponse.Data.UserId;
            viewModel.Bio = profileResponse.Data.Bio;
            viewModel.City = profileResponse.Data.City;
            viewModel.School = profileResponse.Data.School;
            viewModel.FollowerCount = profileResponse.Data.FollowerCount;
            viewModel.FollowingCount = profileResponse.Data.FollowingCount;

            HttpContext.Session.SetString("UserName", profileResponse.Data.FullName);
            HttpContext.Session.SetString("UserAvatarUrl", profileResponse.Data.AvatarUrl ?? string.Empty);
        }

        private async Task LoadFollowersAsync(HomeFeedViewModel viewModel)
        {
            var followersResponse = await _userApiService.GetMyFollowersAsync(40);
            if (!followersResponse.IsSuccess || followersResponse.Data is null)
            {
                if (string.IsNullOrWhiteSpace(viewModel.ProfileError))
                {
                    viewModel.ProfileError = followersResponse.ErrorMessage ?? string.Empty;
                }
                return;
            }

            viewModel.FollowingUsers = followersResponse.Data
                .Select(user => new FeedUserViewModel
                {
                    UserId = user.UserId,
                    DisplayName = string.IsNullOrWhiteSpace(user.FullName) ? "Người dùng" : user.FullName,
                    AvatarUrl = user.AvatarUrl ?? string.Empty
                })
                .ToList();
        }

        private static string ResolveTarget(string? source)
        {
            return string.Equals(source, "profile", StringComparison.OrdinalIgnoreCase)
                ? nameof(Profile)
                : nameof(Index);
        }
    }
}
