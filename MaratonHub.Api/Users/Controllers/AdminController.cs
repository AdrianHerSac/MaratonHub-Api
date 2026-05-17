using MaratonHub.Api.Reviews.Reposytory;
using MaratonHub.Api.Users.Models;
using MaratonHub.Api.Users.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaratonHub.Api.Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IReviewRepository _reviewRepository;

        public AdminController(IUserRepository userRepository, IReviewRepository reviewRepository)
        {
            _userRepository = userRepository;
            _reviewRepository = reviewRepository;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var users = await _userRepository.GetAllUsersAsync();
            var reviewCount = await _reviewRepository.CountReviewsAsync();
            
            var connectedUsers = users.Count(u => u.LastLogin.HasValue && u.LastLogin.Value > DateTime.UtcNow.AddMinutes(-30));
            var recentUsers = users.OrderByDescending(u => u.CreatedAt).Take(5).Select(u => new {
                u.Username,
                u.CreatedAt,
                IsOnline = u.LastLogin.HasValue && u.LastLogin.Value > DateTime.UtcNow.AddMinutes(-30)
            });

            return Ok(new
            {
                TotalUsers = users.Count(),
                TotalReviews = reviewCount,
                ConnectedUsers = connectedUsers,
                SystemStatus = "Operativo",
                LastUpdate = DateTime.UtcNow,
                RecentUsers = recentUsers
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return Ok(users.Select(u => new {
                u.Id,
                u.Username,
                u.Role,
                u.CreatedAt,
                u.GoogleId
            }));
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var success = await _userRepository.DeleteUserAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] string newRole)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            if (newRole != UserRoles.Admin && newRole != UserRoles.User)
                return BadRequest("Rol inválido.");

            user.Role = newRole;
            await _userRepository.UpdateUserAsync(user);
            return Ok(user);
        }
    }
}
