using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DashboardController(AppDbContext context) => _context = context;

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.UtcNow.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var totalStudents = await _context.Students.CountAsync();
            var totalTeachers = await _context.Teachers.CountAsync();
            var totalClasses = await _context.Classes.CountAsync();
            var newStudentsMonth = await _context.Students.CountAsync(s => s.CreatedAt >= thisMonth);
            var newTeachersMonth = await _context.Teachers.CountAsync(t => t.CreatedAt >= thisMonth);

            // Attendance today
            var totalToday = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == today);
            var presentToday = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == today && a.Status == "Present");
            var attendancePercent = totalToday > 0
                ? Math.Round((double)presentToday / totalToday * 100, 1) : 0;

            // Notices
            var totalNotices = await _context.Notices.CountAsync();
            var recentNotices = await _context.Notices
                .OrderByDescending(n => n.NoticeDate)
                .Take(5)
                .Select(n => new { n.Id, n.Title, n.Description, n.NoticeDate })
                .ToListAsync();

            // Attendance trend last 7 days
            var attendanceTrend = new List<object>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var total = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == date);
                var present = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == date && a.Status == "Present");
                var percent = total > 0 ? Math.Round((double)present / total * 100, 1) : 0;
                attendanceTrend.Add(new
                {
                    Date = date.ToString("MMM dd"),
                    Total = total,
                    Present = present,
                    Percent = percent
                });
            }

            // Students per class
            var studentsPerClass = await _context.Classes
                .Select(c => new {
                    ClassName = c.ClassName + " - " + c.Section,
                    Count = _context.Students.Count(s => s.ClassId == c.Id)
                })
                .Where(x => x.Count > 0)
                .ToListAsync();

            // Recent Students — last 5
            var recentStudents = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new {
                    s.Id,
                    FullName = s.User!.FullName,
                    s.RollNo,
                    ClassName = s.Class!.ClassName + " - " + s.Class.Section,
                    s.CreatedAt
                })
                .ToListAsync();

            // Recent Teachers — last 5
            var recentTeachers = await _context.Teachers
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new {
                    t.Id,
                    FullName = t.User!.FullName,
                    t.EmployeeCode,
                    t.Qualification,
                    t.CreatedAt
                })
                .ToListAsync();

            // Activity Feed — combine recent actions
            var activityFeed = new List<object>();

            // Recent students as activity
            foreach (var s in recentStudents)
                activityFeed.Add(new
                {
                    Type = "student",
                    Icon = "bi-person-plus-fill",
                    Color = "#2980b9",
                    Message = $"New student added: {s.FullName}",
                    SubText = s.ClassName,
                    CreatedAt = s.CreatedAt
                });

            // Recent teachers as activity
            foreach (var t in recentTeachers)
                activityFeed.Add(new
                {
                    Type = "teacher",
                    Icon = "bi-person-badge-fill",
                    Color = "#27ae60",
                    Message = $"New teacher added: {t.FullName}",
                    SubText = t.Qualification,
                    CreatedAt = t.CreatedAt
                });

            // Recent notices as activity
            foreach (var n in recentNotices)
                activityFeed.Add(new
                {
                    Type = "notice",
                    Icon = "bi-megaphone-fill",
                    Color = "#e67e22",
                    Message = $"Notice posted: {n.Title}",
                    SubText = "",
                    CreatedAt = n.NoticeDate
                });

            // Sort by date
            var sortedFeed = activityFeed
                .OrderByDescending(a => ((dynamic)a).CreatedAt)
                .Take(10)
                .ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    TotalStudents = totalStudents,
                    TotalTeachers = totalTeachers,
                    TotalClasses = totalClasses,
                    NewStudentsMonth = newStudentsMonth,
                    NewTeachersMonth = newTeachersMonth,
                    AttendanceToday = attendancePercent,
                    PresentToday = presentToday,
                    TotalToday = totalToday,
                    TotalNotices = totalNotices,
                    RecentNotices = recentNotices,
                    RecentStudents = recentStudents,
                    RecentTeachers = recentTeachers,
                    ActivityFeed = sortedFeed,
                    AttendanceTrend = attendanceTrend,
                    StudentsPerClass = studentsPerClass
                }
            });
        }
    }
}