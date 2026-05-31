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
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProfileController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.GetUserId();
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            // Extra info based on role
            object? roleInfo = null;

            if (user.Role?.RoleName == "Teacher")
            {
                roleInfo = await _context.Teachers
                    .Where(t => t.UserId == userId)
                    .Select(t => new {
                        t.EmployeeCode,
                        t.Qualification,
                        t.JoiningDate,
                        t.Salary,
                        AssignedClasses = _context.TeacherClasses
                            .Where(tc => tc.TeacherId == t.Id && tc.IsActive)
                            .Select(tc => tc.Class!.ClassName + " - " + tc.Class.Section)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();
            }
            else if (user.Role?.RoleName == "Student")
            {
                roleInfo = await _context.Students
                    .Include(s => s.Class)
                    .Where(s => s.UserId == userId)
                    .Select(s => new {
                        s.RollNo,
                        s.FatherName,
                        s.PhoneNo,
                        s.Address,
                        s.DOB,
                        s.AdmissionDate,
                        ClassName = s.Class!.ClassName + " - " + s.Class.Section
                    })
                    .FirstOrDefaultAsync();
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    Role = user.Role?.RoleName,
                    user.IsActive,
                    user.CreatedAt,
                    RoleInfo = roleInfo
                }
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            // Check email duplicate
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email && u.Id != userId);
            if (emailExists)
                return BadRequest(new { success = false, message = "Email already in use!" });

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.UpdatedBy = userId;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Profile updated successfully" });
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { success = false, message = "Current password is incorrect!" });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { success = false, message = "Passwords do not match!" });

            if (dto.NewPassword.Length < 6)
                return BadRequest(new { success = false, message = "Password must be at least 6 characters!" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedBy = userId;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Password changed successfully!" });
        }
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}