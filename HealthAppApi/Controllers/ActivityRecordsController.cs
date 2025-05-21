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

        [HttpPost]
        public async Task<ActionResult<Record>> CreateRecord([FromBody] Record newRecord)
        {
            _logger.LogInformation("Received POST to /api/ActivityRecords with body: {@Record}", newRecord);
            if (newRecord == null)
            {
                _logger.LogWarning("Request body is null");
                return BadRequest(new { errors = new { message = "Request body is required" } });
            }

            if (string.IsNullOrEmpty(newRecord.UserId) || string.IsNullOrEmpty(newRecord.ActivityType) || 
                string.IsNullOrEmpty(newRecord.Mood) || string.IsNullOrEmpty(newRecord.Duration) || 
                newRecord.Exercises == null)
            {
                _logger.LogWarning("Invalid record data: Required fields are missing");
                return BadRequest(new { errors = new { message = "UserId, ActivityType, Mood, Duration, and Exercises are required" } });
            }

            newRecord.Created_At = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid: {Errors}", ModelState);
                return BadRequest(new { errors = ModelState });
            }

            _context.Records.Add(newRecord);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Record created for user: {UserId}", newRecord.UserId);
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

        [HttpGet("{id}")]
        public async Task<ActionResult<Record>> GetRecord(int id)
        {
            _logger.LogInformation("Received GET to /api/ActivityRecords/{Id}", id);
            var record = await _context.Records
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null)
            {
                _logger.LogWarning("Record not found: {Id}", id);
                return NotFound("Record not found");
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetRecords()
        {
            _logger.LogInformation("Received GET to /api/ActivityRecords");
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
            return Ok(records);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserRecords(string userId)
        {
            _logger.LogInformation("Received GET to /api/ActivityRecords/user/{UserId}", userId);
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
            return Ok(records);
        }

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
    }
}