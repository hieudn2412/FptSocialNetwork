using DataAccessLayer.Services.Models;
using FptSocialNetwork.Client.Models;
using FptSocialNetwork.Client.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FptSocialNetwork.Client.Controllers
{
    [Route("conversations")]
    public class ConversationController : Controller
    {
        private readonly IConversationApiService _conversationApiService;
        private readonly IMessageApiService _messageApiService;
        private readonly IUserApiService _userApiService;
        private readonly ICloudinaryUploadService _cloudinaryUploadService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public ConversationController(
            IConversationApiService conversationApiService,
            IMessageApiService messageApiService,
            IUserApiService userApiService,
            ICloudinaryUploadService cloudinaryUploadService,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _conversationApiService = conversationApiService;
            _messageApiService = messageApiService;
            _userApiService = userApiService;
            _cloudinaryUploadService = cloudinaryUploadService;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            int? conversationId = null,
            string? tab = null,
            string? q = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (userId is null || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new ConversationPageViewModel
            {
                CurrentUserId = userId.Value,
                SelectedConversationId = conversationId,
                Tab = tab,
                Keyword = q,
                ErrorMessage = TempData["ConversationError"] as string,
                ShouldScrollToBottom = (TempData["ShouldScrollToBottom"] as bool?) == true,
                RealtimeToken = token,
                ChatHubUrl = BuildChatHubUrl()
            };

            var conversationsResult = await _conversationApiService.GetConversationsAsync(tab, null);
            if (!conversationsResult.IsSuccess)
            {
                if (IsUnauthorized(conversationsResult.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                model.ErrorMessage = conversationsResult.ErrorMessage;
                return View(model);
            }

            model.Conversations = conversationsResult.Data ?? new List<ConversationListItemDto>();
            if (model.Conversations.Count == 0)
            {
                return View(model);
            }

            var selectedConversationId = conversationId ?? model.Conversations.First().Id;
            model.SelectedConversationId = selectedConversationId;

            var detailResult = await _conversationApiService.GetConversationDetailAsync(selectedConversationId);
            if (!detailResult.IsSuccess)
            {
                if (IsUnauthorized(detailResult.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                model.ErrorMessage = detailResult.ErrorMessage;
                return View(model);
            }

            model.SelectedConversation = detailResult.Data;

            var messageResult = await _messageApiService.GetMessagesAsync(selectedConversationId);
            if (!messageResult.IsSuccess)
            {
                if (IsUnauthorized(messageResult.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                model.ErrorMessage = messageResult.ErrorMessage;
                return View(model);
            }

            model.Messages = messageResult.Data ?? new List<MessageDto>();

            // Update unread state after successfully opening conversation messages.
            await _conversationApiService.MarkConversationAsReadAsync(selectedConversationId);

            return View(model);
        }

        [HttpPost]
        [Route("send")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            int conversationId,
            string? content,
            long? replyToMessageId = null,
            List<IFormFile>? attachments = null,
            string? tab = null,
            string? q = null)
        {
            var isAjax = string.Equals(
                Request.Headers["X-Requested-With"].ToString(),
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                if (isAjax)
                {
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return RedirectToAction("Login", "Account");
            }

            var userId = sessionUserId.Value;
            var hasTextContent = !string.IsNullOrWhiteSpace(content);
            var hasFiles = attachments is { Count: > 0 };

            if (conversationId <= 0)
            {
                if (isAjax)
                {
                    return BadRequest(new { error = "ConversationId is invalid." });
                }

                return RedirectToAction(nameof(Index), new { tab, q });
            }

            if (!hasTextContent && !hasFiles)
            {
                if (isAjax)
                {
                    return BadRequest(new { error = "Message content or attachment is required." });
                }

                return RedirectToAction(nameof(Index), new { conversationId, tab, q });
            }

            var attachmentRequests = hasFiles
                ? await SaveAttachmentsAsync(attachments!)
                : new List<SendMessageAttachmentRequest>();

            var request = new SendMessageRequest
            {
                ConversationId = conversationId,
                SenderId = userId,
                ReplyToMessageId = replyToMessageId,
                Content = hasTextContent ? content!.Trim() : string.Empty,
                MessageType = attachmentRequests.Count > 0 ? "media" : "text",
                Attachments = attachmentRequests
            };

            var result = await _messageApiService.SendMessageAsync(request);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    if (isAjax)
                    {
                        return Unauthorized(new { error = "Unauthorized." });
                    }

                    return RedirectToAction("Login", "Account");
                }

                if (isAjax)
                {
                    return BadRequest(new { error = result.ErrorMessage ?? "Cannot send message." });
                }

                TempData["ConversationError"] = result.ErrorMessage;
            }

            if (result.IsSuccess)
            {
                TempData["ShouldScrollToBottom"] = true;
                if (isAjax)
                {
                    return Ok(result.Data);
                }
            }

            return RedirectToAction(nameof(Index), new { conversationId, tab, q });
        }

        [HttpPost("message/{messageId:long}/reaction")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReaction(long messageId, string reactionType = "like")
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            var result = await _messageApiService.ToggleReactionAsync(messageId, reactionType);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot update reaction." });
            }

            return Ok(result.Data);
        }

        [HttpPost("message/{messageId:long}/unsend")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsendMessage(long messageId)
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            var result = await _messageApiService.UnsendMessageAsync(messageId);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot unsend message." });
            }

            return Ok(result.Data);
        }

        [HttpPost("message/{messageId:long}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(long messageId, string content)
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            var result = await _messageApiService.EditMessageAsync(messageId, content);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot edit message." });
            }

            return Ok(result.Data);
        }

        [HttpPost("message/{messageId:long}/hide")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForMe(long messageId)
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            var result = await _messageApiService.DeleteForMeAsync(messageId);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot hide message." });
            }

            return NoContent();
        }

        [HttpGet("messages-json")]
        public async Task<IActionResult> GetMessagesJson(int conversationId, int page = 1, int pageSize = 50, bool markRead = true)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (conversationId <= 0)
            {
                return BadRequest(new { error = "ConversationId is invalid." });
            }

            var result = await _messageApiService.GetMessagesAsync(conversationId, page, pageSize);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot load messages." });
            }

            if (markRead)
            {
                await _conversationApiService.MarkConversationAsReadAsync(conversationId);
            }

            return Ok(result.Data ?? new List<MessageDto>());
        }

        [HttpGet("detail-json")]
        public async Task<IActionResult> GetConversationDetailJson(int conversationId)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (conversationId <= 0)
            {
                return BadRequest(new { error = "ConversationId is invalid." });
            }

            var result = await _conversationApiService.GetConversationDetailAsync(conversationId);
            if (!result.IsSuccess || result.Data is null)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot load conversation detail." });
            }

            return Ok(result.Data);
        }

        [HttpGet("list-json")]
        public async Task<IActionResult> GetConversationListJson(string? tab = null, string? q = null)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            var result = await _conversationApiService.GetConversationsAsync(tab, q);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot load conversations." });
            }

            return Ok(result.Data ?? new List<ConversationListItemDto>());
        }

        [HttpGet("users-search-json")]
        public async Task<IActionResult> SearchUsersJson(string? q)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new List<UserSearchItemDto>());
            }

            var result = await _userApiService.SearchUsersAsync(q);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot search users." });
            }

            return Ok(result.Data ?? new List<UserSearchItemDto>());
        }

        [HttpGet("search-messages-json")]
        public async Task<IActionResult> SearchMessagesJson(int conversationId, string? q, int page = 1, int pageSize = 30)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (conversationId <= 0)
            {
                return BadRequest(new { error = "ConversationId is invalid." });
            }

            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new MessageSearchResultDto());
            }

            var result = await _messageApiService.SearchMessagesAsync(conversationId, q, page, pageSize);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot search messages." });
            }

            return Ok(result.Data ?? new MessageSearchResultDto());
        }

        [HttpPost("create-group-json")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroupJson(string? name, List<int>? memberUserIds)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            var normalizedMemberIds = (memberUserIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (normalizedMemberIds.Count == 0)
            {
                return BadRequest(new { error = "Vui lòng chọn ít nhất 1 người để tạo nhóm." });
            }

            var request = new CreateConversationRequest
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Nhóm chat mới" : name.Trim(),
                Type = "group",
                MemberUserIds = normalizedMemberIds
            };

            var result = await _conversationApiService.CreateConversationAsync(request);
            if (!result.IsSuccess || result.Data is null)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Không thể tạo nhóm chat." });
            }

            return Ok(new
            {
                conversationId = result.Data.Id
            });
        }

        [HttpGet("start-user/{targetUserId:int}")]
        public async Task<IActionResult> StartConversationWithUser(int targetUserId)
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _conversationApiService.GetOrCreateDirectConversationAsync(targetUserId);
            if (!result.IsSuccess || result.Data is null)
            {
                TempData["ConversationError"] = result.ErrorMessage ?? "Cannot open conversation with this user.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index), new { conversationId = result.Data.Id });
        }

        [HttpPost("update-settings-json")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateConversationSettingsJson(int conversationId, string? name, string? avatarUrl)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (conversationId <= 0)
            {
                return BadRequest(new { error = "ConversationId is invalid." });
            }

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(avatarUrl))
            {
                return BadRequest(new { error = "Nothing to update." });
            }

            var result = await _conversationApiService.UpdateConversationSettingsAsync(conversationId, name, avatarUrl);
            if (!result.IsSuccess || result.Data is null)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot update conversation settings." });
            }

            return Ok(new
            {
                id = result.Data.Id,
                name = result.Data.Name,
                avatarUrl = result.Data.AvatarUrl,
                type = result.Data.Type
            });
        }

        [HttpPost("update-image-json")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateConversationImageJson([FromForm] int conversationId, [FromForm] IFormFile? image)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (conversationId <= 0)
            {
                return BadRequest(new { error = "ConversationId is invalid." });
            }

            if (image is null || image.Length <= 0)
            {
                return BadRequest(new { error = "Image is empty." });
            }

            try
            {
                var uploadedUrl = await _cloudinaryUploadService.UploadImageAsync(image);
                var result = await _conversationApiService.UpdateConversationSettingsAsync(conversationId, null, uploadedUrl);
                if (!result.IsSuccess || result.Data is null)
                {
                    if (IsUnauthorized(result.StatusCode))
                    {
                        HttpContext.Session.Clear();
                        return Unauthorized(new { error = "Unauthorized." });
                    }

                    return BadRequest(new { error = result.ErrorMessage ?? "Cannot update conversation image." });
                }

                return Ok(new
                {
                    id = result.Data.Id,
                    name = result.Data.Name,
                    avatarUrl = result.Data.AvatarUrl
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("add-members-json")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMembersJson(int conversationId, List<int>? memberUserIds)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (conversationId <= 0)
            {
                return BadRequest(new { error = "ConversationId is invalid." });
            }

            var normalizedIds = (memberUserIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            if (normalizedIds.Count == 0)
            {
                return BadRequest(new { error = "No member selected." });
            }

            var result = await _conversationApiService.AddMembersAsync(conversationId, normalizedIds);
            if (!result.IsSuccess || result.Data is null)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot add members." });
            }

            return Ok(new
            {
                conversationId = result.Data.Id,
                memberCount = result.Data.Members.Count
            });
        }

        [HttpPost("delete-conversation-json")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConversationJson(int conversationId)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var token = HttpContext.Session.GetString("AccessToken");
            if (sessionUserId is null || string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { error = "Unauthorized." });
            }

            if (conversationId <= 0)
            {
                return BadRequest(new { error = "ConversationId is invalid." });
            }

            var result = await _conversationApiService.DeleteConversationAsync(conversationId);
            if (!result.IsSuccess)
            {
                if (IsUnauthorized(result.StatusCode))
                {
                    HttpContext.Session.Clear();
                    return Unauthorized(new { error = "Unauthorized." });
                }

                return BadRequest(new { error = result.ErrorMessage ?? "Cannot delete conversation." });
            }

            return NoContent();
        }

        private string BuildChatHubUrl()
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5156/";
            return $"{apiBaseUrl.TrimEnd('/')}/hubs/chat";
        }

        private async Task<List<SendMessageAttachmentRequest>> SaveAttachmentsAsync(List<IFormFile> files)
        {
            var folderDate = DateTime.UtcNow.ToString("yyyyMMdd");
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var uploadsRoot = Path.Combine(webRootPath, "uploads", "chat", folderDate);
            Directory.CreateDirectory(uploadsRoot);

            var result = new List<SendMessageAttachmentRequest>();
            foreach (var file in files)
            {
                if (file.Length <= 0)
                {
                    continue;
                }

                var ext = Path.GetExtension(file.FileName);
                var safeName = $"{Guid.NewGuid():N}{ext}";
                var physicalPath = Path.Combine(uploadsRoot, safeName);
                await using var stream = new FileStream(physicalPath, FileMode.Create);
                await file.CopyToAsync(stream);

                var relativeUrl = $"/uploads/chat/{folderDate}/{safeName}";
                var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
                result.Add(new SendMessageAttachmentRequest
                {
                    FileName = Path.GetFileName(file.FileName),
                    FileUrl = absoluteUrl,
                    AttachmentType = ResolveAttachmentType(file.ContentType, file.FileName)
                });
            }

            return result;
        }

        private static string ResolveAttachmentType(string? contentType, string fileName)
        {
            var normalized = contentType?.ToLowerInvariant() ?? string.Empty;
            if (normalized.StartsWith("image/"))
            {
                return "image";
            }

            if (normalized.StartsWith("video/"))
            {
                return "video";
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" => "image",
                ".mp4" or ".mov" or ".avi" or ".webm" => "video",
                _ => "file"
            };
        }

        private static bool IsUnauthorized(int statusCode)
        {
            return statusCode == 401;
        }
    }
}
