using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    [ApiController]
    [Route("api/[controller]")]
    public class ExamsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ExamsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var exams = await _context.Exams
                .Include(e => e.Class)
                .Select(e => new {
                    e.Id,
                    e.ExamName,
                    e.ClassId,
                    ClassName = e.Class!.ClassName + " - " + e.Class.Section,
                    e.ExamDate,
                    e.TotalMarks,
                    
                    e.IsActive
                })
                .ToListAsync();
            return Ok(new { success = true, data = exams });
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateExamDto dto)
        {
            try
            {
                var exam = new Exam
                {
                    ExamName = dto.ExamName,
                    ClassId = dto.ClassId,
                    ExamDate = dto.ExamDate,
                    TotalMarks = dto.TotalMarks,
                    
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Exams.Add(exam);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, data = exam.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateExamDto dto)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
                return NotFound(new { success = false, message = "Exam not found" });

            exam.ExamName = dto.ExamName;
            exam.ClassId = dto.ClassId;
            exam.ExamDate = dto.ExamDate;
            exam.TotalMarks = dto.TotalMarks;
            
            exam.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
                return NotFound(new { success = false, message = "Exam not found" });

            exam.IsActive = false;
            exam.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }

        public class CreateExamDto
        {
            public string ExamName { get; set; } = string.Empty;
            public int ClassId { get; set; }
            public DateTime ExamDate { get; set; }
            public decimal TotalMarks { get; set; }
        }
    }
}