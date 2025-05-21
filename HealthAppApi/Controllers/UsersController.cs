using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthAppApi.Data;
using HealthAppApi.Models;

namespace HealthAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly HealthAppContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(HealthAppContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/Users/login
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            _logger.LogInformation("Received POST to /api/Users/login");
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Id) || string.IsNullOrEmpty(loginRequest.Password))
            {
                _logger.LogWarning("Invalid login request: ID and password are required");
                return BadRequest(new { errors = new { message = "ID and Password are required" } });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == loginRequest.Id);

            if (user == null || user.Password != loginRequest.Password)
            {
                _logger.LogWarning("Invalid credentials for user: {Id}", loginRequest.Id);
                return Unauthorized("Invalid ID or Password");
            }

            _logger.LogInformation("User logged in: {Id}", loginRequest.Id);
            return Ok(new
            {
                user.Id,
                user.Name,
                user.Age,
                user.Sex,
                user.Created_At,
                user.IsAdmin,
                role = user.IsAdmin ? "admin" : "user"
            });
        }

        // POST: api/Users/register
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] User newUser)
        {
            _logger.LogInformation("Received POST to /api/Users/register with body: {@User}", newUser);
            if (newUser == null)
            {
                _logger.LogWarning("Request body is null");
                return BadRequest(new { errors = new { message = "Request body is required" } });
            }

            if (string.IsNullOrEmpty(newUser.Id) || string.IsNullOrEmpty(newUser.Password) || string.IsNullOrEmpty(newUser.Name))
            {
                _logger.LogWarning("Invalid user data: ID, Password, and Name are required");
                return BadRequest(new { errors = new { message = "ID, Password, and Name are required" } });
            }

            if (newUser.Id.Length > 50 || newUser.Name.Length > 100 || newUser.Password.Length > 255)
            {
                _logger.LogWarning("Invalid user data: ID, Name, or Password exceeds length limits");
                return BadRequest(new { errors = new { message = "ID (max 50), Name (max 100), or Password (max 255) too long" } });
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == newUser.Id);
            if (existingUser != null)
            {
                _logger.LogWarning("User ID already exists: {Id}", newUser.Id);
                return Conflict(new { errors = new { message = "User ID already exists" } });
            }

            newUser.Name = newUser.Name.Trim();
            newUser.Sex = string.IsNullOrEmpty(newUser.Sex) ? "Unknown" : newUser.Sex.Trim();
            newUser.Created_At = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid: {Errors}", ModelState);
                return BadRequest(new { errors = ModelState });
            }

            _context.Users.Add(newUser);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User registered: {Id}", newUser.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error saving user: {Id}", newUser.Id);
                return StatusCode(500, new { errors = new { message = "Database error: " + (ex.InnerException?.Message ?? ex.Message) } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving user: {Id}", newUser.Id);
                return StatusCode(500, new { errors = new { message = "Unexpected error: " + ex.Message } });
            }

            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, new
            {
                newUser.Id,
                newUser.Name,
                newUser.Age,
                newUser.Sex,
                newUser.Created_At,
                newUser.IsAdmin
            });
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            _logger.LogInformation("Received GET to /api/Users/{Id}", id);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                _logger.LogWarning("User not found: {Id}", id);
                return NotFound("User not found");
            }

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Age,
                user.Sex,
                user.Created_At,
                user.IsAdmin
            });
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] User updatedUser)
        {
            _logger.LogInformation("Received PUT to /api/Users/{Id}", id);
            if (id != updatedUser.Id)
            {
                _logger.LogWarning("ID mismatch: {Id}", id);
                return BadRequest("ID mismatch");
            }

            if (string.IsNullOrEmpty(updatedUser.Password))
            {
                _logger.LogWarning("Password is required for user: {Id}", id);
                return BadRequest("Password is required");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Id}", id);
                return NotFound("User not found");
            }

            if (user.Password != updatedUser.Password)
            {
                _logger.LogWarning("Invalid password for user: {Id}", id);
                return BadRequest("Invalid password");
            }

            user.Name = updatedUser.Name?.Trim() ?? user.Name;
            user.Age = updatedUser.Age;
            user.Sex = updatedUser.Sex?.Trim() ?? user.Sex;
            user.IsAdmin = updatedUser.IsAdmin;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User updated: {Id}", id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Users.AnyAsync(u => u.Id == id))
                {
                    _logger.LogWarning("User not found during update: {Id}", id);
                    return NotFound();
                }
                throw;
            }
            return NoContent();
        }

        // POST: api/Users/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            _logger.LogInformation("Received POST to /api/Users/reset-password");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return NotFound("User not found");
            }

            if (string.IsNullOrEmpty(request.NewPassword))
            {
                _logger.LogWarning("New password is required for user: {UserId}", request.UserId);
                return BadRequest("New password is required");
            }

            user.Password = request.NewPassword;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset for user: {UserId}", request.UserId);
            return Ok("Password reset successfully");
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            _logger.LogInformation("Received DELETE to /api/Users/{Id}", id);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Id}", id);
                return NotFound("User not found");
            }

            var records = _context.Records.Where(r => r.UserId == id);
            _context.Records.RemoveRange(records);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User deleted: {Id}", id);
            return NoContent();
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            _logger.LogInformation("Received GET to /api/Users");
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Age,
                    u.Sex,
                    u.Created_At,
                    u.IsAdmin
                })
                .ToListAsync();
            return Ok(users);
        }

        // GET: api/Users/stars
        [HttpGet("stars")]
        public async Task<IActionResult> GetUserStars()
        {
            _logger.LogInformation("Received GET to /api/Users/stars");
            var users = await _context.Users
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();
            var result = new List<object>();
            foreach (var user in users)
            {
                var records = await _context.Records
                    .Where(r => r.UserId == user.Id)
                    .Select(r => r.ActivityType)
                    .Distinct()
                    .ToListAsync();
                result.Add(new
                {
                    username = user.Name ?? user.Id,
                    starCount = records.Count
                });
            }
            return Ok(result);
        }

        public class LoginRequest
        {
            public string Id { get; set; }
            public string Password { get; set; }
        }

        public class ResetPasswordRequest
        {
            public string UserId { get; set; }
            public string NewPassword { get; set; }
        }
    }
}