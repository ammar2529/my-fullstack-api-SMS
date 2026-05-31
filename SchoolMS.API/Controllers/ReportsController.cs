using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Infrastructure.Data;
using SchoolMS.API.Extensions;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReportsController(AppDbContext context) => _context = context;

        // =============================================
        // STUDENT LIST REPORT
        // =============================================
        [HttpGet("students")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetStudentReport(
            [FromQuery] int? classId,
            [FromQuery] string? search)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    s.User!.FullName.Contains(search) ||
                    s.RollNo.Contains(search));

            var students = await query
                .OrderBy(s => s.Class!.ClassName)
                .ThenBy(s => s.RollNo)
                .Select(s => new {
                    s.Id,
                    s.RollNo,
                    FullName = s.User!.FullName,
                    s.FatherName,
                    s.PhoneNo,
                    ClassName = s.Class!.ClassName + " - " + s.Class.Section,
                    s.DOB,
                    s.AdmissionDate,
                    s.Address
                })
                .ToListAsync();

            return Ok(new { success = true, data = students });
        }

        // =============================================
        // ATTENDANCE REPORT
        // =============================================
        [HttpGet("attendance")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetAttendanceReport(
            [FromQuery] int? classId,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate)
        {
            var from = string.IsNullOrEmpty(fromDate)
                ? DateTime.UtcNow.AddDays(-30)
                : DateTime.Parse(fromDate);
            var to = string.IsNullOrEmpty(toDate)
                ? DateTime.UtcNow
                : DateTime.Parse(toDate);

            var query = _context.Attendances
                .Include(a => a.Student).ThenInclude(s => s!.User)
                .Include(a => a.Class)
                .Where(a => a.AttendanceDate >= from && a.AttendanceDate <= to)
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(a => a.ClassId == classId);

            var attendance = await query
                .OrderBy(a => a.AttendanceDate)
                .ThenBy(a => a.Student!.RollNo)
                .Select(a => new {
                    a.Id,
                    StudentName = a.Student!.User!.FullName,
                    RollNo = a.Student!.RollNo,
                    ClassName = a.Class!.ClassName + " - " + a.Class.Section,
                    a.AttendanceDate,
                    a.Status,
                })
                .ToListAsync();

            // Summary per student
            var summary = attendance
                .GroupBy(a => new { a.RollNo, a.StudentName, a.ClassName })
                .Select(g => new {
                    g.Key.RollNo,
                    g.Key.StudentName,
                    g.Key.ClassName,
                    Total = g.Count(),
                    Present = g.Count(a => a.Status == "Present"),
                    Absent = g.Count(a => a.Status == "Absent"),
                    Leave = g.Count(a => a.Status == "Leave"),
                    Percent = Math.Round((double)g.Count(a => a.Status == "Present") / g.Count() * 100, 1)
                })
                .OrderBy(s => s.ClassName)
                .ThenBy(s => s.RollNo)
                .ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    Detail = attendance,
                    Summary = summary,
                    FromDate = from.ToString("yyyy-MM-dd"),
                    ToDate = to.ToString("yyyy-MM-dd")
                }
            });
        }

        // =============================================
        // EXAM RESULTS REPORT
        // =============================================
        [HttpGet("results")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetResultsReport(
            [FromQuery] int? examId,
            [FromQuery] int? classId)
        {
            var query = _context.ExamResults
                .Include(r => r.Exam).ThenInclude(e => e!.Class)
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Subject)
                .AsQueryable();

            if (examId.HasValue)
                query = query.Where(r => r.ExamId == examId);

            if (classId.HasValue)
                query = query.Where(r => r.Exam!.ClassId == classId);

            var results = await query
                .OrderBy(r => r.Student!.RollNo)
                .ThenBy(r => r.Subject!.SubjectName)
                .Select(r => new {
                    r.Id,
                    ExamName = r.Exam!.ExamName,
                    ClassName = r.Exam!.Class!.ClassName + " - " + r.Exam!.Class.Section,
                    StudentName = r.Student!.User!.FullName,
                    RollNo = r.Student!.RollNo,
                    SubjectName = r.Subject!.SubjectName,
                    r.ObtainedMarks,
                    TotalMarks = r.Exam!.TotalMarks,
                    r.Grade,
                    Percentage = Math.Round((double)(r.ObtainedMarks / r.Exam!.TotalMarks) * 100, 1)
                })
                .ToListAsync();

            // Summary per student
            var summary = results
                .GroupBy(r => new { r.RollNo, r.StudentName, r.ClassName, r.ExamName })
                .Select(g => new {
                    g.Key.RollNo,
                    g.Key.StudentName,
                    g.Key.ClassName,
                    g.Key.ExamName,
                    TotalObtained = g.Sum(r => r.ObtainedMarks),
                    TotalMarks = g.Sum(r => r.TotalMarks),
                    Percentage = Math.Round((double)g.Sum(r => r.ObtainedMarks) / g.Sum(r => (double)r.TotalMarks) * 100, 1),
                    Grade = GetOverallGrade(g.Sum(r => r.ObtainedMarks), g.Sum(r => r.TotalMarks))
                })
                .OrderByDescending(s => s.Percentage)
                .ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    Detail = results,
                    Summary = summary
                }
            });
        }

        private static string GetOverallGrade(decimal obtained, decimal total)
        {
            var pct = (double)(obtained / total) * 100;
            return pct switch
            {
                >= 90 => "A+",
                >= 80 => "A",
                >= 70 => "B",
                >= 60 => "C",
                >= 50 => "D",
                _ => "F"
            };
        }
    }
}