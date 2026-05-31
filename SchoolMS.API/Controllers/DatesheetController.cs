using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;
using SchoolMS.API.Extensions;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DatesheetController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DatesheetController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Datesheets
                .Include(d => d.Class)
                .Include(d => d.Subject)
                .Where(d => d.IsActive)
                .OrderBy(d => d.ExamDate)
                .ToListAsync();

            var result = list.Select(d => new {
                d.Id,
                d.ExamTitle,
                d.ClassId,
                ClassName = d.Class!.ClassName + " - " + d.Class.Section,
                d.SubjectId,
                SubjectName = d.Subject!.SubjectName,
                d.ExamDate,
                StartTime = d.StartTime.ToString(@"hh\:mm"),
                EndTime = d.EndTime.ToString(@"hh\:mm"),
                d.Venue,
                d.Notes,
                d.IsActive
            });

            return Ok(new { success = true, data = result });
        }

        [HttpGet("byexamtitle/{examTitle}/{classId}")]
        public async Task<IActionResult> GetByExamAndClass(string examTitle, int classId)
        {
            var list = await _context.Datesheets
                .Include(d => d.Subject)
                .Where(d => d.ExamTitle == examTitle && d.ClassId == classId && d.IsActive)
                .OrderBy(d => d.ExamDate)
                .ToListAsync();

            var result = list.Select(d => new {
                d.Id,
                d.SubjectId,
                SubjectName = d.Subject!.SubjectName,
                d.ExamDate,
                StartTime = d.StartTime.ToString(@"hh\:mm"),
                EndTime = d.EndTime.ToString(@"hh\:mm"),
                d.Venue,
                d.Notes
            });

            return Ok(new { success = true, data = result });
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkSave([FromBody] BulkDatesheetDto dto)
        {
            var userId = User.GetUserId();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Purani entries delete karo same exam+class ki
                var existing = await _context.Datesheets
                    .Where(d => d.ExamTitle == dto.ExamTitle && d.ClassId == dto.ClassId)
                    .ToListAsync();
                foreach (var e in existing)
                {
                    e.IsActive = false;
                    e.UpdatedAt = DateTime.UtcNow;
                    e.UpdatedBy = userId;
                }

                // Nayi entries add karo
                foreach (var item in dto.Items)
                {
                    if (string.IsNullOrEmpty(item.StartTime) || string.IsNullOrEmpty(item.EndTime))
                        continue;

                    var datesheet = new Datesheet
                    {
                        ExamTitle = dto.ExamTitle,
                        ClassId = dto.ClassId,
                        SubjectId = item.SubjectId,
                        ExamDate = item.ExamDate,
                        StartTime = TimeOnly.Parse(item.StartTime),
                        EndTime = TimeOnly.Parse(item.EndTime),
                        Venue = item.Venue ?? "",
                        Notes = item.Notes ?? "",
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Datesheets.Add(datesheet);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Datesheet saved successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("byexamtitle/{examTitle}/{classId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteByExam(string examTitle, int classId)
        {
            var userId = User.GetUserId();
            var list = await _context.Datesheets
                .Where(d => d.ExamTitle == examTitle && d.ClassId == classId)
                .ToListAsync();

            foreach (var d in list)
            {
                d.IsActive = false;
                d.UpdatedAt = DateTime.UtcNow;
                d.UpdatedBy = userId;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }
    }

    public class BulkDatesheetDto
    {
        public string ExamTitle { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public List<DatesheetItem> Items { get; set; } = new();
    }

    public class DatesheetItem
    {
        public int SubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string? Venue { get; set; }
        public string? Notes { get; set; }
    }
}