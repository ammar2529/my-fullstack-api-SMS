using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TimetableController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TimetableController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var timetable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.Subject)
                .Include(t => t.Teacher).ThenInclude(t => t!.User)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync(); // Pehle list mein lao

            // Phir client side pe format karo
            var result = timetable.Select(t => new {
                t.Id,
                t.ClassId,
                ClassName = t.Class!.ClassName + " - " + t.Class.Section,
                t.SubjectId,
                SubjectName = t.Subject!.SubjectName,
                t.TeacherId,
                TeacherName = t.Teacher!.User!.FullName,
                t.DayOfWeek,
                StartTime = t.StartTime.ToString(@"hh\:mm"),
                EndTime = t.EndTime.ToString(@"hh\:mm"),
                t.IsActive
            });

            return Ok(new { success = true, data = result });
        }

        [HttpGet("byclass/{classId}")]
        public async Task<IActionResult> GetByClass(int classId)
        {
            var timetable = await _context.Timetables
                .Include(t => t.Subject)
                .Include(t => t.Teacher).ThenInclude(t => t!.User)
                .Where(t => t.ClassId == classId)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync(); // Pehle list mein lao

            var result = timetable.Select(t => new {
                t.Id,
                t.ClassId,
                t.SubjectId,
                SubjectName = t.Subject!.SubjectName,
                t.TeacherId,
                TeacherName = t.Teacher!.User!.FullName,
                t.DayOfWeek,
                StartTime = t.StartTime.ToString(@"hh\:mm"),
                EndTime = t.EndTime.ToString(@"hh\:mm"),
                t.IsActive
            });

            return Ok(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTimetableDto dto)
        {
            try
            {
                // Validate time
                if (string.IsNullOrEmpty(dto.StartTime) || string.IsNullOrEmpty(dto.EndTime))
                    return BadRequest(new { success = false, message = "Start and End time required" });

                var startTime = TimeOnly.Parse(dto.StartTime);
                var endTime = TimeOnly.Parse(dto.EndTime);

                if (endTime <= startTime)
                    return BadRequest(new { success = false, message = "End time must be after start time" });

                // Check conflict — client side mein karo
                var existing = await _context.Timetables
                    .Where(t => t.ClassId == dto.ClassId && t.DayOfWeek == dto.DayOfWeek && t.IsActive)
                    .ToListAsync();

                var conflict = existing.Any(t =>
                    (startTime >= t.StartTime && startTime < t.EndTime) ||
                    (endTime > t.StartTime && endTime <= t.EndTime)
                );

                if (conflict)
                    return BadRequest(new { success = false, message = "Time slot conflict for this class!" });

                var timetable = new Timetable
                {
                    ClassId = dto.ClassId,
                    SubjectId = dto.SubjectId,
                    TeacherId = dto.TeacherId,
                    DayOfWeek = dto.DayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Timetables.Add(timetable);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, data = timetable.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTimetableDto dto)
        {
            try
            {
                var timetable = await _context.Timetables.FindAsync(id);
                if (timetable == null)
                    return NotFound(new { success = false, message = "Not found" });

                if (string.IsNullOrEmpty(dto.StartTime) || string.IsNullOrEmpty(dto.EndTime))
                    return BadRequest(new { success = false, message = "Start and End time required" });

                timetable.ClassId = dto.ClassId;
                timetable.SubjectId = dto.SubjectId;
                timetable.TeacherId = dto.TeacherId;
                timetable.DayOfWeek = dto.DayOfWeek;
                timetable.StartTime = TimeOnly.Parse(dto.StartTime);
                timetable.EndTime = TimeOnly.Parse(dto.EndTime);
                timetable.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var timetable = await _context.Timetables.FindAsync(id);
            if (timetable == null)
                return NotFound(new { success = false, message = "Not found" });

            timetable.IsActive = false;
            timetable.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }
    }

    public class CreateTimetableDto
    {
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }
}