using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SearchController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrEmpty(q) || q.Length < 2)
                return Ok(new { success = true, data = new List<object>() });

            var results = new List<object>();
            q = q.ToLower();

            // Students
            var students = await _context.Students
                .Include(s => s.User).Include(s => s.Class)
                .Where(s => s.User!.FullName.ToLower().Contains(q) ||
                            s.RollNo.ToLower().Contains(q))
                .Take(5)
                .Select(s => new {
                    Type = "Student",
                    Title = s.User!.FullName,
                    Subtitle = $"Roll: {s.RollNo} — {s.Class!.ClassName}",
                    Route = "/students",
                    Icon = "bi-person-fill",
                    Color = "#2980b9"
                }).ToListAsync();
            results.AddRange(students);

            // Teachers
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.User!.FullName.ToLower().Contains(q) ||
                            t.EmployeeCode.ToLower().Contains(q))
                .Take(3)
                .Select(t => new {
                    Type = "Teacher",
                    Title = t.User!.FullName,
                    Subtitle = $"Code: {t.EmployeeCode}",
                    Route = "/teachers",
                    Icon = "bi-person-badge-fill",
                    Color = "#27ae60"
                }).ToListAsync();
            results.AddRange(teachers);

            // Notices
            var notices = await _context.Notices
                .Where(n => n.Title.ToLower().Contains(q) ||
                            n.Description.ToLower().Contains(q))
                .Take(3)
                .Select(n => new {
                    Type = "Notice",
                    Title = n.Title,
                    Subtitle = n.Description.Length > 50
                        ? n.Description.Substring(0, 50) + "..."
                        : n.Description,
                    Route = "/notices",
                    Icon = "bi-megaphone-fill",
                    Color = "#e67e22"
                }).ToListAsync();
            results.AddRange(notices);

            // Classes
            var classes = await _context.Classes
                .Where(c => c.ClassName.ToLower().Contains(q) ||
                            c.Section.ToLower().Contains(q))
                .Take(3)
                .Select(c => new {
                    Type = "Class",
                    Title = c.ClassName + " - " + c.Section,
                    Subtitle = $"Class ID: {c.Id}",
                    Route = "/classes",
                    Icon = "bi-building",
                    Color = "#8e44ad"
                }).ToListAsync();
            results.AddRange(classes);

            return Ok(new { success = true, data = results });
        }
    }
}