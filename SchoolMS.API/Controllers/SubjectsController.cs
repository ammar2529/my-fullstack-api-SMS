using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class SubjectsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SubjectsController(AppDbContext context) => _context = context;

        [HttpGet]
       
        public async Task<IActionResult> GetAll()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Class)
                .Select(s => new {
                    s.Id,
                    s.SubjectName,
                    s.ClassId,
                    ClassName = s.Class!.ClassName + " - " + s.Class.Section,
                    s.TotalMarks,
                    s.IsActive
                })
                .ToListAsync();
            return Ok(new { success = true, data = subjects });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubjectDto dto)
        {
            var subject = new Subject
            {
                SubjectName = dto.SubjectName,
                ClassId = dto.ClassId,
                TotalMarks = dto.TotalMarks,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = subject.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSubjectDto dto)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound(new { success = false, message = "Subject not found" });

            subject.SubjectName = dto.SubjectName;
            subject.ClassId = dto.ClassId;
            subject.TotalMarks = dto.TotalMarks;
            subject.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated successfully" });
        }

        public class CreateSubjectDto
        {
            public string SubjectName { get; set; } = string.Empty;
            public int ClassId { get; set; }
            public decimal TotalMarks { get; set; } = 100;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound(new { success = false, message = "Subject not found" });

            subject.IsActive = false;
            subject.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }
    }
}