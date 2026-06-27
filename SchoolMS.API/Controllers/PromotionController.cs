using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Infrastructure.Data;
using SchoolMS.API.Extensions;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PromotionController(AppDbContext context) => _context = context;

        // Get students with their results for promotion decision
        [HttpGet("students/{classId}/{examId}")]
        public async Task<IActionResult> GetStudentsForPromotion(int classId, int examId)
        {
            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassId == classId && s.IsActive)
                .ToListAsync();

            var results = await _context.ExamResults
                .Include(r => r.Subject)
                .Where(r => r.ExamId == examId)
                .ToListAsync();

            var data = students.Select(s => {
                var studentResults = results.Where(r => r.StudentId == s.Id).ToList();
                var totalObtained = studentResults.Sum(r => r.ObtainedMarks);
                var totalMarks = studentResults.Sum(r => r.Exam?.TotalMarks ?? 0);
                var percentage = totalMarks > 0
                    ? Math.Round((double)totalObtained / (double)totalMarks * 100, 1) : 0;
                var hasFailed = studentResults.Any(r => {
                    var pct = r.Exam?.TotalMarks > 0
                        ? (double)r.ObtainedMarks / (double)r.Exam.TotalMarks * 100 : 0;
                    return pct < 40;
                });

                return new
                {
                    s.Id,
                    s.RollNo,
                    FullName = s.User!.FullName,
                    s.ClassId,
                    TotalObtained = totalObtained,
                    TotalMarks = totalMarks,
                    Percentage = percentage,
                    HasFailed = hasFailed,
                    Status = hasFailed ? "Fail" : percentage >= 40 ? "Pass" : "Fail",
                    PromoteStatus = hasFailed ? "Detain" : "Promote", // Default suggestion
                    ResultCount = studentResults.Count
                };
            }).OrderBy(s => s.RollNo).ToList();

            return Ok(new { success = true, data });
        }

        // Get next class options
        [HttpGet("nextclasses/{classId}")]
        public async Task<IActionResult> GetNextClasses(int classId)
        {
            var currentClass = await _context.Classes.FindAsync(classId);
            if (currentClass == null)
                return NotFound(new { success = false, message = "Class not found" });

            // Same class (repeat) + other classes as next
            var classes = await _context.Classes
                .Where(c => c.IsActive)
                .Select(c => new {
                    c.Id,
                    c.ClassName,
                    c.Section,
                    DisplayName = c.ClassName + " - " + c.Section,
                    IsSame = c.Id == classId
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = classes,
                currentClass = new
                {
                    currentClass.Id,
                    currentClass.ClassName,
                    currentClass.Section
                }
            });
        }

        // Process promotion
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPromotion([FromBody] ProcessPromotionDto dto)
        {
            var userId = User.GetUserId();
            var success = 0;
            var errors = new List<string>();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in dto.Students)
                {
                    var student = await _context.Students.FindAsync(item.StudentId);
                    if (student == null) continue;

                    if (item.Action == "Promote" && item.NextClassId > 0)
                    {
                        student.ClassId = item.NextClassId;
                        student.UpdatedAt = DateTime.UtcNow;
                        student.UpdatedBy = userId;
                        success++;
                    }
                    else if (item.Action == "Detain")
                    {
                        // Same class mein raho — no change needed
                        success++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Processed = success,
                        Errors = errors,
                        Message = $"{success} students processed successfully!"
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class ProcessPromotionDto
    {
        public List<PromotionItem> Students { get; set; } = new();
    }

    public class PromotionItem
    {
        public int StudentId { get; set; }
        public string Action { get; set; } = "Promote"; // Promote, Detain
        public int NextClassId { get; set; }
    }
}