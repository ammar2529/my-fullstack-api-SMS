using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.API.Extensions;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class TeachersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeachersController(AppDbContext context)
        {
            _context = context;
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
                    t.IsActive
                })
                .ToListAsync();

            return Ok(new { success = true, data = teachers });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTeacherDto dto)
        {
            var userId = User.GetUserId();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? "Teacher@123"),
                    RoleId = 2,
                    IsActive = true,

                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var teacher = new Teacher
                {
                    UserId = user.Id,
                    EmployeeCode = dto.EmployeeCode,
                    Qualification = dto.Qualification,
                    JoiningDate = dto.JoiningDate,
                    Salary = dto.Salary,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { success = true, data = teacher.Id, message = "Teacher created successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTeacherDto dto)
        {

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return NotFound(new { success = false, message = "Teacher not found" });

            teacher.EmployeeCode = dto.EmployeeCode;
            teacher.Qualification = dto.Qualification;
            teacher.JoiningDate = dto.JoiningDate;
            teacher.Salary = dto.Salary;
            teacher.UpdatedAt = DateTime.UtcNow;

            if (teacher.User != null)
            {
                teacher.User.FullName = dto.FullName;
                teacher.User.Email = dto.Email;
                teacher.User.UpdatedAt = DateTime.UtcNow;

            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Teacher updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return NotFound(new { success = false, message = "Teacher not found" });

            teacher.IsActive = false;
            teacher.UpdatedAt = DateTime.UtcNow;

            if (teacher.User != null)
            {
                teacher.User.IsActive = false;
                teacher.User.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Teacher deleted successfully" });
        }
    }

    public class CreateTeacherDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        public decimal Salary { get; set; }
    }

    public class UpdateTeacherDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        public decimal Salary { get; set; }
    }
}