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
    public class TeacherClassesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TeacherClassesController(AppDbContext context) => _context = context;

        [HttpGet("byteacher/{teacherId}")]
        public async Task<IActionResult> GetByTeacher(int teacherId)
        {
            var classes = await _context.TeacherClasses
                .Include(tc => tc.Class)
                .Where(tc => tc.TeacherId == teacherId)
                .Select(tc => new {
                    tc.Id,
                    tc.TeacherId,
                    tc.ClassId,
                    ClassName = tc.Class!.ClassName + " - " + tc.Class.Section
                })
                .ToListAsync();
            return Ok(new { success = true, data = classes });
        }

        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] TeacherClass dto)
        {
            var exists = await _context.TeacherClasses.AnyAsync(tc =>
                tc.TeacherId == dto.TeacherId && tc.ClassId == dto.ClassId);
            if (exists)
                return BadRequest(new { success = false, message = "Already assigned" });

            dto.IsActive = true;
            dto.CreatedAt = DateTime.UtcNow;
            _context.TeacherClasses.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Class assigned successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var tc = await _context.TeacherClasses.FindAsync(id);
            if (tc == null)
                return NotFound(new { success = false, message = "Not found" });
            tc.IsActive = false;
            tc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Removed successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Select(t => new {
                    t.Id,
                    t.EmployeeCode,
                    FullName = t.User!.FullName,
                    Email = t.User!.Email,
                    t.Qualification,
                    t.JoiningDate,
                    t.Salary,
                    t.IsActive,
                    AssignedClasses = _context.TeacherClasses
                        .Where(tc => tc.TeacherId == t.Id && tc.IsActive)
                        .Select(tc => tc.Class!.ClassName + " - " + tc.Class.Section)
                        .ToList()
                })
                .ToListAsync();
            return Ok(new { success = true, data = teachers });
        }
    }
}