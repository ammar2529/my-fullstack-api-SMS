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
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AttendanceController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var attendance = await _context.Attendances
                .Include(a => a.Student).ThenInclude(s => s!.User)
                .Include(a => a.Class)
                .Select(a => new {
                    a.Id,
                    a.StudentId,
                    StudentName = a.Student!.User!.FullName,
                    RollNo = a.Student!.RollNo,
                    a.ClassId,
                    ClassName = a.Class!.ClassName + " - " + a.Class.Section,
                    a.AttendanceDate,
                    a.Status,
                    a.MarkedBy,
                    a.IsActive
                })
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
            return Ok(new { success = true, data = attendance });
        }

        [HttpGet("bydate/{date}")]
        public async Task<IActionResult> GetByDate(DateTime date)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Student).ThenInclude(s => s!.User)
                .Include(a => a.Class)
                .Where(a => a.AttendanceDate.Date == date.Date)
                .Select(a => new {
                    a.Id,
                    a.StudentId,
                    StudentName = a.Student!.User!.FullName,
                    RollNo = a.Student!.RollNo,
                    a.ClassId,
                    ClassName = a.Class!.ClassName + " - " + a.Class.Section,
                    a.AttendanceDate,
                    a.Status,
                    a.IsActive
                })
                .ToListAsync();
            return Ok(new { success = true, data = attendance });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Attendance dto)
        {
            // Check duplicate
            var exists = await _context.Attendances.AnyAsync(a =>
                a.StudentId == dto.StudentId &&
                a.AttendanceDate.Date == dto.AttendanceDate.Date);

            if (exists)
                return BadRequest(new { success = false, message = "Attendance already marked for this student today" });

            dto.IsActive = true;
            dto.CreatedAt = DateTime.UtcNow;
            _context.Attendances.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = dto.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Attendance dto)
        {
            var att = await _context.Attendances.FindAsync(id);
            if (att == null)
                return NotFound(new { success = false, message = "Record not found" });

            att.Status = dto.Status;
            att.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var att = await _context.Attendances.FindAsync(id);
            if (att == null)
                return NotFound(new { success = false, message = "Record not found" });

            att.IsActive = false;
            att.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] List<Attendance> list)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var dto in list)
                {
                    var exists = await _context.Attendances.AnyAsync(a =>
                        a.StudentId == dto.StudentId &&
                        a.AttendanceDate.Date == dto.AttendanceDate.Date);

                    if (exists)
                    {
                        // Update existing
                        var existing = await _context.Attendances.FirstAsync(a =>
                            a.StudentId == dto.StudentId &&
                            a.AttendanceDate.Date == dto.AttendanceDate.Date);
                        existing.Status = dto.Status;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        dto.IsActive = true;
                        dto.CreatedAt = DateTime.UtcNow;
                        _context.Attendances.Add(dto);
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Attendance saved successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("byclass/{classId}/{date}")]
        public async Task<IActionResult> GetByClassAndDate(int classId, string date)
        {
            var parsedDate = DateTime.Parse(date);

            // Sab students of this class
            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassId == classId)
                .Select(s => new {
                    s.Id,
                    s.RollNo,
                    FullName = s.User!.FullName
                })
                .ToListAsync();

            // Existing attendance
            var existing = await _context.Attendances
                .Where(a => a.ClassId == classId && a.AttendanceDate.Date == parsedDate.Date)
                .ToListAsync();

            // Merge
            var result = students.Select(s => new {
                StudentId = s.Id,
                s.RollNo,
                s.FullName,
                ClassId = classId,
                AttendanceDate = parsedDate.ToString("yyyy-MM-dd"),
                Status = existing.FirstOrDefault(a => a.StudentId == s.Id)?.Status ?? "Present",
                AttId = existing.FirstOrDefault(a => a.StudentId == s.Id)?.Id ?? 0
            });

            return Ok(new { success = true, data = result });
        }

        [HttpGet("byteacher/{teacherId}/{date}")]
        public async Task<IActionResult> GetByTeacher(int teacherId, string date)
        {
            var parsedDate = DateTime.Parse(date);

            var teacherClassIds = await _context.TeacherClasses
                .Where(tc => tc.TeacherId == teacherId && tc.IsActive)
                .Select(tc => tc.ClassId)
                .ToListAsync();

            if (!teacherClassIds.Any()) {
                return Ok(new { success = true, data = new List<object>() });
            }
       

            var allRows = new List<object>();

            foreach (var classId in teacherClassIds)
            {
                var students = await _context.Students
                    .Include(s => s.User)
                    .Include(s => s.Class)
                    .Where(s => s.ClassId == classId)
                    .Select(s => new {
                        StudentId = s.Id,
                        s.RollNo,
                        FullName = s.User!.FullName,
                        ClassId = classId,
                        ClassName = s.Class!.ClassName + " - " + s.Class!.Section,
                        AttendanceDate = parsedDate.ToString("yyyy-MM-dd"),
                        Status = _context.Attendances
                            .Where(a => a.StudentId == s.Id && a.AttendanceDate.Date == parsedDate.Date)
                            .Select(a => a.Status)
                            .FirstOrDefault() ?? "Present",
                        AttId = _context.Attendances
                            .Where(a => a.StudentId == s.Id && a.AttendanceDate.Date == parsedDate.Date)
                            .Select(a => a.Id)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                allRows.AddRange(students);
            }

            return Ok(new { success = true, data = allRows });
        }
    }
}