using DataAccessLayer.Services;
using DataAccessLayer.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FptSocialNetwork.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<MyProfileDto>> GetMe()
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var profile = await _userService.GetMyProfileAsync(userId.Value);
            if (profile is null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        [HttpPost("me/profile")]
        public async Task<ActionResult<MyProfileDto>> UpdateMe([FromBody] UpdateMyProfileRequest request)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var updated = await _userService.UpdateMyProfileAsync(userId.Value, request ?? new UpdateMyProfileRequest());
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<UserSearchItemDto>>> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new List<UserSearchItemDto>());
            }

            var userId = ResolveCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var users = await _userService.SearchUsersAsync(userId.Value, q);
            return Ok(users);
        }

        [HttpGet("me/followers")]
        public async Task<ActionResult<List<FollowUserDto>>> GetMyFollowers([FromQuery] int take = 30)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var followers = await _userService.GetFollowersAsync(userId.Value, take);
            return Ok(followers);
        }

        [HttpGet("me/following")]
        public async Task<ActionResult<List<FollowUserDto>>> GetMyFollowing([FromQuery] int take = 30)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var following = await _userService.GetFollowingAsync(userId.Value, take);
            return Ok(following);
        }

        [HttpPost("{targetUserId:int}/follow-toggle")]
        public async Task<ActionResult<FollowToggleResultDto>> ToggleFollow([FromRoute] int targetUserId)
        {
            var userId = ResolveCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            try
            {
                var result = await _userService.ToggleFollowAsync(userId.Value, targetUserId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private int? ResolveCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
