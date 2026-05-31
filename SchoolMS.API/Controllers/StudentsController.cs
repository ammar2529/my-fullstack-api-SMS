using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.API.Extensions;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetAll()
        {

            var userId = User.GetUserId();
            var role = User.GetRole();

            IQueryable<Student> query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Class);

            // Teacher sirf apni class ke students dekhe
            if (role == "Teacher")
            {
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher == null)
                    return Ok(new { success = true, data = new List<object>() });

                var teacherClassIds = await _context.TeacherClasses
                    .Where(tc => tc.TeacherId == teacher.Id && tc.IsActive)
                    .Select(tc => tc.ClassId)
                    .ToListAsync();

                query = query.Where(s => teacherClassIds.Contains(s.ClassId));
            }
            var students = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Select(s => new {
                    s.Id,
                    s.RollNo,
                    FullName = s.User!.FullName,
                    Email = s.User!.Email,
                    ClassName = s.Class!.ClassName,
                    Section = s.Class!.Section,
                    s.FatherName,
                    s.PhoneNo,
                    s.Address,
                    s.DOB,
                    s.AdmissionDate,
                    s.IsActive
                })
                .ToListAsync();

            return Ok(new { success = true, data = students });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            return Ok(new { success = true, data = student });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
        {
            var userId = User.GetUserId();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // User banana
                var user = new User
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? "Student@123"),
                    RoleId = 3, // Student Role
                    IsActive = true,
                    CreatedBy = userId,    // ← Add
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Student banana
                var student = new Student
                {
                    UserId = user.Id,
                    RollNo = dto.RollNo,
                    ClassId = dto.ClassId,
                    FatherName = dto.FatherName,
                    PhoneNo = dto.PhoneNo,
                    Address = dto.Address,
                    DOB = dto.DOB,
                    AdmissionDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedBy = userId,    // ← Add
                    CreatedAt = DateTime.UtcNow
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { success = true, data = student.Id, message = "Student created successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentDto dto)
        {
            var userId = User.GetUserId();
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            student.RollNo = dto.RollNo;
            student.ClassId = dto.ClassId;
            student.FatherName = dto.FatherName;
            student.PhoneNo = dto.PhoneNo;
            student.Address = dto.Address;
            student.DOB = dto.DOB;
            student.UpdatedAt = DateTime.UtcNow;
            student.UpdatedBy = userId;     // ← Add
            student.UpdatedAt = DateTime.UtcNow;

            if (student.User != null)
            {
                student.User.FullName = dto.FullName;
                student.User.Email = dto.Email;
                student.User.UpdatedBy = userId;  // ← Add
                student.User.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Student updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            student.IsActive = false;
            student.UpdatedAt = DateTime.UtcNow;

            if (student.User != null)
            {
                student.User.IsActive = false;
                student.User.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Student deleted successfully" });
        }
    }

    // DTOs
    public class CreateStudentDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string RollNo { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string FatherName { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
    }

    public class UpdateStudentDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RollNo { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string FatherName { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
    }
}

