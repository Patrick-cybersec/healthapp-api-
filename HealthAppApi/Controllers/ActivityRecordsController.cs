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
    public class ActivityRecordsController : ControllerBase
    {
        private readonly HealthAppContext _context;
        private readonly ILogger<ActivityRecordsController> _logger;

        public ActivityRecordsController(HealthAppContext context, ILogger<ActivityRecordsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public static Dictionary<string, List<(string Metric, string Value, string Unit)>> ParseExercises(string exercises)
        {
            var result = new Dictionary<string, List<(string Metric, string Value, string Unit)>>();
            if (string.IsNullOrEmpty(exercises)) return result;

            var exerciseEntries = exercises.Split(", ");
            foreach (var entry in exerciseEntries)
            {
                var parts = entry.Split(": ");
                if (parts.Length != 2) continue;

                var exercise = parts[0].Trim();
                var metricParts = parts[1].Trim().Split(" ");
                if (metricParts.Length < 3) continue;

                var metric = metricParts[0];
                var value = metricParts[1];
                var unit = metricParts[2];

                if (!result.ContainsKey(exercise))
                {
                    result[exercise] = new List<(string Metric, string Value, string Unit)>();
                }
                result[exercise].Add((metric, value, unit));
            }
            return result;
        }

        // POST: api/ActivityRecords
        [HttpPost]
        public async Task<ActionResult<Record>> CreateRecord([FromBody] AdminRecordRequest request)
        {
            _logger.LogInformation("Received POST to /api/ActivityRecords with body: {@Request}", request);
            if (request == null || request.Record == null || string.IsNullOrEmpty(request.AdminId) || string.IsNullOrEmpty(request.AdminPassword))
            {
                _logger.LogWarning("Invalid request: Admin credentials and record data are required");
                return BadRequest(new { errors = new { message = "Admin credentials and record data are required" } });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.AdminId);
            if (user == null || user.Password != request.AdminPassword)
            {
                _logger.LogWarning("Invalid credentials for user: {AdminId}", request.AdminId);
                return Unauthorized("Invalid credentials");
            }
            if (!user.IsAdmin && user.Id != request.Record.UserId)
            {
                _logger.LogWarning("Non-admin user {AdminId} attempted to create record for user {UserId}", request.AdminId, request.Record.UserId);
                return Unauthorized("Non-admin users can only create records for themselves");
            }

            var newRecord = request.Record;
            if (string.IsNullOrEmpty(newRecord.UserId) || string.IsNullOrEmpty(newRecord.ActivityType) || 
                string.IsNullOrEmpty(newRecord.Mood) || string.IsNullOrEmpty(newRecord.Duration))
            {
                _logger.LogWarning("Invalid record data: Required fields are missing");
                return BadRequest(new { errors = new { message = "UserId, ActivityType, Mood, and Duration are required" } });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == newRecord.UserId);
            if (!userExists)
            {
                _logger.LogWarning("UserId does not exist: {UserId}", newRecord.UserId);
                return BadRequest(new { errors = new { message = "UserId does not exist" } });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid: {Errors}", ModelState);
                return BadRequest(new { errors = ModelState });
            }

            newRecord.Created_At = DateTime.UtcNow;

            // Parse Exercises and create Exercise entities
            var parsedExercises = ParseExercises(newRecord.Exercises);
            var exerciseEntities = new List<Exercise>();
            foreach (var exercise in parsedExercises)
            {
                foreach (var (metric, value, unit) in exercise.Value)
                {
                    exerciseEntities.Add(new Exercise
                    {
                        ExerciseName = exercise.Key,
                        Metric = metric,
                        Value = value,
                        Unit = unit
                    });
                }
            }

            _context.Records.Add(newRecord);
            try
            {
                await _context.SaveChangesAsync();

                // Assign RecordId to Exercise entities and save
                foreach (var exercise in exerciseEntities)
                {
                    exercise.RecordId = newRecord.Id;
                    _context.Exercises.Add(exercise);
                }
                await _context.SaveChangesAsync();

                _logger.LogInformation("Record created successfully for user: {UserId}, ActivityType: {ActivityType}, Exercises: {Exercises}, Created_At: {Created_At}",
                    newRecord.UserId, newRecord.ActivityType, newRecord.Exercises, newRecord.Created_At);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error saving record for user: {UserId}", newRecord.UserId);
                return StatusCode(500, new { errors = new { message = "Database error: " + (ex.InnerException?.Message ?? ex.Message) } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving record for user: {UserId}", newRecord.UserId);
                return StatusCode(500, new { errors = new { message = "Unexpected error: " + ex.Message } });
            }

            return CreatedAtAction(nameof(GetRecord), new { id = newRecord.Id }, new
            {
                newRecord.Id,
                newRecord.UserId,
                newRecord.ActivityType,
                newRecord.HeartRate,
                newRecord.Mood,
                newRecord.Duration,
                newRecord.Exercises,
                newRecord.Created_At
            });
        }

        // GET: api/ActivityRecords/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Record>> GetRecord(int id, [FromQuery] string requestingUserId, [FromQuery] string requestingUserPassword)
        {
            _logger.LogInformation("Received GET to /api/ActivityRecords/{Id}", id);
            var requestingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == requestingUserId);
            if (requestingUser == null || requestingUser.Password != requestingUserPassword)
            {
                _logger.LogWarning("Invalid credentials for requesting user: {RequestingUserId}", requestingUserId);
                return Unauthorized("Invalid requesting user credentials");
            }

            var record = await _context.Records.FirstOrDefaultAsync(r => r.Id == id);
            if (record == null)
            {
                _logger.LogWarning("Record not found: {Id}", id);
                return NotFound("Record not found");
            }

            if (!requestingUser.IsAdmin && requestingUserId != record.UserId)
            {
                _logger.LogWarning("Non-admin user {RequestingUserId} attempted to access record {Id}", requestingUserId, id);
                return Forbid("Non-admin users can only access their own records");
            }

            return Ok(new
            {
                record.Id,
                record.UserId,
                record.ActivityType,
                record.HeartRate,
                record.Mood,
                record.Duration,
                record.Exercises,
                record.Created_At
            });
        }

        // GET: api/ActivityRecords
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetRecords([FromQuery] string adminId, [FromQuery] string adminPassword)
        {
            _logger.LogInformation("Received GET to /api/ActivityRecords");
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminId && u.IsAdmin);
            if (admin == null || admin.Password != adminPassword)
            {
                _logger.LogWarning("Invalid admin credentials for admin: {AdminId}", adminId);
                return Unauthorized("Invalid admin credentials");
            }

            var records = await _context.Records
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    r.ActivityType,
                    r.HeartRate,
                    r.Mood,
                    r.Duration,
                    r.Exercises,
                    r.Created_At
                })
                .ToListAsync();
            _logger.LogInformation("Fetched {Count} records for admin: {AdminId}", records.Count, adminId);
            return Ok(records);
        }

        // GET: api/ActivityRecords/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserRecords(string userId, [FromQuery] string requestingUserId, [FromQuery] string requestingUserPassword)
        {
            _logger.LogInformation("Received GET to /api/ActivityRecords/user/{UserId}", userId);
            var requestingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == requestingUserId);
            if (requestingUser == null || requestingUser.Password != requestingUserPassword)
            {
                _logger.LogWarning("Invalid credentials for requesting user: {RequestingUserId}", requestingUserId);
                return Unauthorized("Invalid requesting user credentials");
            }

            if (!requestingUser.IsAdmin && requestingUserId != userId)
            {
                _logger.LogWarning("Non-admin user {RequestingUserId} attempted to access records for user {UserId}", requestingUserId, userId);
                return Forbid("Non-admin users can only access their own records");
            }

            var records = await _context.Records
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    r.ActivityType,
                    r.HeartRate,
                    r.Mood,
                    r.Duration,
                    r.Exercises,
                    r.Created_At
                })
                .ToListAsync();
            _logger.LogInformation("Fetched {Count} records for user: {UserId}", records.Count, userId);
            return Ok(records);
        }

        // PUT: api/ActivityRecords/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecord(int id, [FromBody] AdminRecordRequest request)
        {
            _logger.LogInformation("Received PUT to /api/ActivityRecords/{Id} with body: {@Request}", id, request);
            if (request == null || request.Record == null || string.IsNullOrEmpty(request.AdminId) || string.IsNullOrEmpty(request.AdminPassword))
            {
                _logger.LogWarning("Invalid update request: Admin credentials and record data are required");
                return BadRequest(new { errors = new { message = "Admin credentials and record data are required" } });
            }

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.AdminId && u.IsAdmin);
            if (admin == null || admin.Password != request.AdminPassword)
            {
                _logger.LogWarning("Invalid admin credentials for admin: {AdminId}", request.AdminId);
                return Unauthorized("Invalid admin credentials");
            }

            var record = await _context.Records.FirstOrDefaultAsync(r => r.Id == id);
            if (record == null)
            {
                _logger.LogWarning("Record not found: {Id}", id);
                return NotFound("Record not found");
            }

            var updatedRecord = request.Record;
            if (id != updatedRecord.Id)
            {
                _logger.LogWarning("ID mismatch: {Id}", id);
                return BadRequest("ID mismatch");
            }

            record.UserId = updatedRecord.UserId?.Trim() ?? record.UserId;
            record.ActivityType = updatedRecord.ActivityType?.Trim() ?? record.ActivityType;
            record.HeartRate = updatedRecord.HeartRate != 0 ? updatedRecord.HeartRate : record.HeartRate;
            record.Mood = updatedRecord.Mood?.Trim() ?? record.Mood;
            record.Duration = updatedRecord.Duration?.Trim() ?? record.Duration;
            record.Exercises = updatedRecord.Exercises?.Trim() ?? record.Exercises;

            // Update Exercise entities
            var existingExercises = await _context.Exercises.Where(e => e.RecordId == id).ToListAsync();
            _context.Exercises.RemoveRange(existingExercises);
            var parsedExercises = ParseExercises(record.Exercises);
            var exerciseEntities = new List<Exercise>();
            foreach (var exercise in parsedExercises)
            {
                foreach (var (metric, value, unit) in exercise.Value)
                {
                    exerciseEntities.Add(new Exercise
                    {
                        RecordId = id,
                        ExerciseName = exercise.Key,
                        Metric = metric,
                        Value = value,
                        Unit = unit
                    });
                }
            }
            _context.Exercises.AddRange(exerciseEntities);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Record updated successfully: {Id}, UserId: {UserId}, ActivityType: {ActivityType}", id, record.UserId, record.ActivityType);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Records.AnyAsync(r => r.Id == id))
                {
                    _logger.LogWarning("Record not found during update: {Id}", id);
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating record: {Id}", id);
                return StatusCode(500, new { errors = new { message = "Unexpected error: " + ex.Message } });
            }
            return NoContent();
        }

        // DELETE: api/ActivityRecords/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecord(int id, [FromBody] AdminLoginRequest request)
        {
            _logger.LogInformation("Received DELETE to /api/ActivityRecords/{Id}", id);
            if (request == null || string.IsNullOrEmpty(request.AdminId) || string.IsNullOrEmpty(request.AdminPassword))
            {
                _logger.LogWarning("Invalid delete request: Admin credentials are required");
                return BadRequest(new { errors = new { message = "Admin credentials are required" } });
            }

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.AdminId && u.IsAdmin);
            if (admin == null || admin.Password != request.AdminPassword)
            {
                _logger.LogWarning("Invalid admin credentials for admin: {AdminId}", request.AdminId);
                return Unauthorized("Invalid admin credentials");
            }

            var record = await _context.Records.FirstOrDefaultAsync(r => r.Id == id);
            if (record == null)
            {
                _logger.LogWarning("Record not found: {Id}", id);
                return NotFound("Record not found");
            }

            var exercises = _context.Exercises.Where(e => e.RecordId == id);
            _context.Exercises.RemoveRange(exercises);
            _context.Records.Remove(record);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Record deleted successfully: {Id}, UserId: {UserId}", id, record.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting record: {Id}", id);
                return StatusCode(500, new { errors = new { message = "Unexpected error: " + ex.Message } });
            }
            return NoContent();
        }

        // POST: api/ActivityRecords/billboard/update
        [HttpPost("billboard/update")]
        public async Task<IActionResult> UpdateBillboard([FromBody] List<BillboardRecord> billboardRecords)
        {
            _logger.LogInformation("Received POST to /api/ActivityRecords/billboard/update with body: {@BillboardRecords}", billboardRecords);
            if (billboardRecords == null || !billboardRecords.Any())
            {
                _logger.LogWarning("Invalid billboard data: Request body is empty");
                return BadRequest(new { errors = new { message = "Billboard records are required" } });
            }

            var updatedRecords = new List<BillboardRecord>();
            foreach (var record in billboardRecords)
            {
                if (string.IsNullOrEmpty(record.SongTitle) || string.IsNullOrEmpty(record.Artist) || record.ChartRank <= 0)
                {
                    _logger.LogWarning("Invalid billboard record: SongTitle, Artist, and valid ChartRank are required");
                    continue;
                }

                var existingRecord = await _context.BillboardRecords
                    .FirstOrDefaultAsync(r => r.ChartRank == record.ChartRank);

                if (existingRecord != null)
                {
                    existingRecord.SongTitle = record.SongTitle.Trim();
                    existingRecord.Artist = record.Artist.Trim();
                    existingRecord.StarNumber = record.StarNumber;
                    existingRecord.Updated_At = DateTime.UtcNow;
                }
                else
                {
                    record.Updated_At = DateTime.UtcNow;
                    _context.BillboardRecords.Add(record);
                }
                updatedRecords.Add(record);
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Billboard records updated: {Count} records", updatedRecords.Count);
                return Ok(new
                {
                    message = "Billboard records updated",
                    updatedRecords = updatedRecords.Select(r => new
                    {
                        r.Id,
                        r.SongTitle,
                        r.Artist,
                        r.ChartRank,
                        r.StarNumber,
                        r.Updated_At
                    })
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating billboard records");
                return StatusCode(500, new { errors = new { message = "Database error: " + (ex.InnerException?.Message ?? ex.Message) } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating billboard records");
                return StatusCode(500, new { errors = new { message = "Unexpected error: " + ex.Message } });
            }
        }

        public class AdminRecordRequest
        {
            public string AdminId { get; set; }
            public string AdminPassword { get; set; }
            public Record Record { get; set; }
        }

        public class AdminLoginRequest
        {
            public string AdminId { get; set; }
            public string AdminPassword { get; set; }
        }
    }
}