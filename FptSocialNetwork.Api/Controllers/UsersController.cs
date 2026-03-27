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

        [HttpGet("search")]
        public async Task<ActionResult<List<UserSearchItemDto>>> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new List<UserSearchItemDto>());
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var users = await _userService.SearchUsersAsync(userId, q);
            return Ok(users);
        }
    }
}
