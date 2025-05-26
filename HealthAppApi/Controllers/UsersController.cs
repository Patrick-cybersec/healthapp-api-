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
        public async Task<ActionResult<User>> Register([FromBody] AdminUserRequest adminUserRequest)
        {
            _logger.LogInformation("Received POST to /api/Users/register");
            if (adminUserRequest == null || adminUserRequest.User == null || string.IsNullOrEmpty(adminUserRequest.AdminId) || string.IsNullOrEmpty(adminUserRequest.AdminPassword))
            {
                _logger.LogWarning("Invalid register request: Admin credentials and user data are required");
                return BadRequest(new { errors = new { message = "Admin credentials and user data are required" } });
            }

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserRequest.AdminId && u.IsAdmin);
            if (admin == null || admin.Password != adminUserRequest.AdminPassword)
            {
                _logger.LogWarning("Invalid admin credentials for admin: {AdminId}", adminUserRequest.AdminId);
                return Unauthorized("Invalid admin credentials");
            }

            var newUser = adminUserRequest.User;
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

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == newUser.Id);
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
        public async Task<ActionResult<User>> GetUser(string id, [FromQuery] string requestingUserId, [FromQuery] string requestingUserPassword)
        {
            _logger.LogInformation("Received GET to /api/Users/{Id}", id);
            var requestingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == requestingUserId);
            if (requestingUser == null || requestingUser.Password != requestingUserPassword)
            {
                _logger.LogWarning("Invalid credentials for requesting user: {RequestingUserId}", requestingUserId);
                return Unauthorized("Invalid requesting user credentials");
            }

            if (!requestingUser.IsAdmin && requestingUserId != id)
            {
                _logger.LogWarning("Non-admin user {RequestingUserId} attempted to access user {Id}", requestingUserId, id);
                return Forbid("Non-admin users can only access their own data");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
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
public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUpdateUserRequest adminUserRequest)
{
    _logger.LogInformation("Received PUT to /api/Users/{0} with body: {@1}", id, adminUserRequest);
    if (adminUserRequest == null || adminUserRequest.User == null || string.IsNullOrEmpty(adminUserRequest.AdminId) || string.IsNullOrEmpty(adminUserRequest.AdminPassword))
    {
        _logger.LogWarning("Invalid update request: Admin credentials and user data are required");
        return BadRequest(new { errors = new { message = "Admin credentials and user data are required" } });
    }

    var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserRequest.AdminId && u.IsAdmin);
    if (admin == null || admin.Password != adminUserRequest.AdminPassword)
    {
        _logger.LogWarning("Invalid admin credentials for admin: {0}", adminUserRequest.AdminId);
        return Unauthorized("Invalid admin credentials");
    }

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    if (user == null)
    {
        _logger.LogWarning("User not found: {0}", id);
        return NotFound("User not found");
    }

    var updatedUser = adminUserRequest.User;
    if (id != updatedUser.Id)
    {
        _logger.LogWarning("ID mismatch: {0}", id);
        return BadRequest("ID mismatch");
    }

    _logger.LogInformation("Updating user Id={0}, Name={1}, Age={2}, Sex={3}, IsAdmin={4}, PasswordProvided={5}",
        id, updatedUser.Name ?? "null", updatedUser.Age, updatedUser.Sex ?? "null", updatedUser.IsAdmin, !string.IsNullOrEmpty(updatedUser.Password));

    user.Name = updatedUser.Name?.Trim() ?? user.Name;
    user.Age = updatedUser.Age > 0 ? updatedUser.Age : user.Age;
    user.Sex = updatedUser.Sex?.Trim() ?? user.Sex;
    user.IsAdmin = updatedUser.IsAdmin;
    if (!string.IsNullOrEmpty(updatedUser.Password))
    {
        user.Password = updatedUser.Password.Trim();
    }

    try
    {
        await _context.SaveChangesAsync();
        _logger.LogInformation("User updated: {0}", id);
    }
    catch (DbUpdateConcurrencyException)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == id))
        {
            _logger.LogWarning("User not found during update: {0}", id);
            return NotFound();
        }
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating user: {0}", id);
        return StatusCode(500, new { errors = new { message = "Unexpected error: " + ex.Message } });
    }
    return NoContent();
}

        // POST: api/Users/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] AdminResetPasswordRequest request)
        {
            _logger.LogInformation("Received POST to /api/Users/reset-password");
            if (request == null || string.IsNullOrEmpty(request.AdminId) || string.IsNullOrEmpty(request.AdminPassword) || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.NewPassword))
            {
                _logger.LogWarning("Invalid reset password request: All fields are required");
                return BadRequest(new { errors = new { message = "Admin credentials, UserId, and NewPassword are required" } });
            }

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.AdminId && u.IsAdmin);
            if (admin == null || admin.Password != request.AdminPassword)
            {
                _logger.LogWarning("Invalid admin credentials for admin: {AdminId}", request.AdminId);
                return Unauthorized("Invalid admin credentials");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return NotFound("User not found");
            }

            user.Password = request.NewPassword;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset for user: {UserId}", request.UserId);
            return Ok("Password reset successfully");
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id, [FromBody] AdminLoginRequest adminRequest)
        {
            _logger.LogInformation("Received DELETE to /api/Users/{Id}", id);
            if (adminRequest == null || string.IsNullOrEmpty(adminRequest.AdminId) || string.IsNullOrEmpty(adminRequest.AdminPassword))
            {
                _logger.LogWarning("Invalid delete request: Admin credentials are required");
                return BadRequest(new { errors = new { message = "Admin credentials are required" } });
            }

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminRequest.AdminId && u.IsAdmin);
            if (admin == null || admin.Password != adminRequest.AdminPassword)
            {
                _logger.LogWarning("Invalid admin credentials for admin: {AdminId}", adminRequest.AdminId);
                return Unauthorized("Invalid admin credentials");
            }

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
        public async Task<ActionResult<IEnumerable<object>>> GetUsers([FromQuery] string adminId, [FromQuery] string adminPassword)
        {
            _logger.LogInformation("Received GET to /api/Users");
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId && u.IsAdmin);
            if (admin == null || admin.Password != adminPassword)
            {
                _logger.LogWarning("Invalid admin credentials for admin: {AdminId}", adminId);
                return Unauthorized("Invalid admin credentials");
            }

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

        // POST: api/Users/public-register
[HttpPost("public-register")]
public async Task<ActionResult<User>> PublicRegister([FromBody] User newUser)
{
    _logger.LogInformation("Received POST to /api/Users/public-register");
    
    // Validate input
    if (newUser == null || string.IsNullOrEmpty(newUser.Id) || string.IsNullOrEmpty(newUser.Password) || string.IsNullOrEmpty(newUser.Name))
    {
        _logger.LogWarning("Invalid user data: ID, Password, and Name are required");
        return BadRequest(new { errors = new { message = "ID, Password, and Name are required" } });
    }

    // Enforce length constraints
    if (newUser.Id.Length > 50 || newUser.Name.Length > 100 || newUser.Password.Length > 255)
    {
        _logger.LogWarning("Invalid user data: ID, Name, or Password exceeds length limits");
        return BadRequest(new { errors = new { message = "ID (max 50), Name (max 100), or Password (max 255) too long" } });
    }

    // Check for existing user
    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == newUser.Id);
    if (existingUser != null)
    {
        _logger.LogWarning("User ID already exists: {Id}", newUser.Id);
        return Conflict(new { errors = new { message = "User ID already exists" } });
    }

    // Set defaults
    newUser.Name = newUser.Name.Trim();
    newUser.Sex = string.IsNullOrEmpty(newUser.Sex) ? "Unknown" : newUser.Sex.Trim();
    newUser.Created_At = DateTime.UtcNow;
    newUser.IsAdmin = false; // Enforce non-admin for public registration
    newUser.Age = newUser.Age >= 0 ? newUser.Age : 0; // Ensure valid age

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

        // GET: api/Users/stars
        [HttpGet("stars")]
        public async Task<IActionResult> GetUserStars()
        {
            _logger.LogInformation("Received GET to /api/Users/stars");
            try
            {
                var userStars = await _context.Users
                    .GroupJoin(
                        _context.Records,
                        user => user.Id,
                        record => record.UserId,
                        (user, records) => new
                        {
                            username = user.Name ?? user.Id,
                            starCount = records.Select(r => r.ActivityType).Distinct().Count()
                        }
                    )
                    .Where(us => us.starCount > 0)
                    .OrderByDescending(us => us.starCount)
                    .ToListAsync();

                _logger.LogInformation("Returning {Count} user star counts", userStars.Count);
                return Ok(userStars);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user stars");
                return StatusCode(500, new { errors = new { message = "Unexpected error: " + ex.Message } });
            }
        }

        public class LoginRequest
        {
            public string Id { get; set; }
            public string Password { get; set; }
        }

        public class AdminLoginRequest
        {
            public string AdminId { get; set; }
            public string AdminPassword { get; set; }
        }

        public class UpdateUserRequest
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string Sex { get; set; }
            public bool IsAdmin { get; set; }
            public string? Password { get; set; } // Optional
        }

        public class AdminUserRequest
        {
            public string AdminId { get; set; }
            public string AdminPassword { get; set; }
            public User User { get; set; }
        }

        public class AdminUpdateUserRequest
        {
            public string AdminId { get; set; }
            public string AdminPassword { get; set; }
            public UpdateUserRequest User { get; set; }
        }

        public class AdminResetPasswordRequest
        {
            public string AdminId { get; set; }
            public string AdminPassword { get; set; }
            public string UserId { get; set; }
            public string NewPassword { get; set; }
        }
    }
}