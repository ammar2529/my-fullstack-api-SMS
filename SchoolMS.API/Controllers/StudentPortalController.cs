using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Infrastructure.Data;
using SchoolMS.API.Extensions;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Student")]
    [ApiController]
    [Route("api/[controller]")]
    public class StudentPortalController : ControllerBase
    {
        private readonly AppDbContext _context;
        public StudentPortalController(AppDbContext context) => _context = context;

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.GetUserId();
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    student.Id,
                    student.RollNo,
                    FullName = student.User!.FullName,
                    Email = student.User!.Email,
                    ClassName = student.Class!.ClassName + " - " + student.Class.Section,
                    student.FatherName,
                    student.PhoneNo,
                    student.Address,
                    student.DOB,
                    student.AdmissionDate
                }
            });
        }

        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendance([FromQuery] string? month)
        {
            var userId = User.GetUserId();
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            var query = _context.Attendances
                .Where(a => a.StudentId == student.Id);

            if (!string.IsNullOrEmpty(month))
            {
                var date = DateTime.Parse(month + "-01");
                query = query.Where(a =>
                    a.AttendanceDate.Year == date.Year &&
                    a.AttendanceDate.Month == date.Month);
            }

            var attendance = await query
                .OrderByDescending(a => a.AttendanceDate)
                .Select(a => new {
                    a.Id,
                    a.AttendanceDate,
                    a.Status
                })
                .ToListAsync();

            var summary = new
            {
                Total = attendance.Count,
                Present = attendance.Count(a => a.Status == "Present"),
                Absent = attendance.Count(a => a.Status == "Absent"),
                Leave = attendance.Count(a => a.Status == "Leave"),
                Percent = attendance.Count > 0
                    ? Math.Round((double)attendance.Count(a => a.Status == "Present") / attendance.Count * 100, 1)
                    : 0
            };

            return Ok(new { success = true, data = new { attendance, summary } });
        }

        [HttpGet("results")]
        public async Task<IActionResult> GetResults()
        {
            var userId = User.GetUserId();
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            var results = await _context.ExamResults
                .Include(r => r.Exam)
                .Include(r => r.Subject)
                .Where(r => r.StudentId == student.Id)
                .OrderByDescending(r => r.Exam!.ExamDate)
                .Select(r => new {
                    r.Id,
                    ExamName = r.Exam!.ExamName,
                    SubjectName = r.Subject!.SubjectName,
                    r.ObtainedMarks,
                    TotalMarks = r.Exam!.TotalMarks,
                    r.Grade,
                    Percentage = Math.Round((double)(r.ObtainedMarks / r.Exam!.TotalMarks) * 100, 1),
                    ExamDate = r.Exam!.ExamDate
                })
                .ToListAsync();

            // Group by exam
            var grouped = results
                .GroupBy(r => r.ExamName)
                .Select(g => new {
                    ExamName = g.Key,
                    Subjects = g.ToList(),
                    TotalObtained = g.Sum(r => r.ObtainedMarks),
                    TotalMarks = g.Sum(r => r.TotalMarks),
                    Percentage = Math.Round((double)g.Sum(r => r.ObtainedMarks) / g.Sum(r => (double)r.TotalMarks) * 100, 1)
                })
                .ToList();

            return Ok(new { success = true, data = grouped });
        }
    }
}