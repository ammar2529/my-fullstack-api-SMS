using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;
using SchoolMS.API.Extensions;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class BulkImportController : ControllerBase
    {
        private readonly AppDbContext _context;
        public BulkImportController(AppDbContext context) => _context = context;

        [HttpPost("students")]
        public async Task<IActionResult> ImportStudents([FromBody] List<ImportStudentDto> students)
        {
            var userId = User.GetUserId();
            var success = 0;
            var errors = new List<string>();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var dto in students)
                {
                    try
                    {
                        // Check duplicate email
                        if (_context.Users.Any(u => u.Email == dto.Email))
                        {
                            errors.Add($"Row {dto.RowNo}: Email {dto.Email} already exists");
                            continue;
                        }

                        var user = new User
                        {
                            FullName = dto.FullName,
                            Email = dto.Email,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                            RoleId = 3,
                            IsActive = true,
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Users.Add(user);
                        await _context.SaveChangesAsync();

                        var student = new Student
                        {
                            UserId = user.Id,
                            RollNo = dto.RollNo,
                            ClassId = dto.ClassId,
                            FatherName = dto.FatherName,
                            PhoneNo = dto.PhoneNo,
                            Address = dto.Address ?? "",
                            DOB = dto.DOB,
                            AdmissionDate = DateTime.UtcNow,
                            IsActive = true,
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Students.Add(student);
                        await _context.SaveChangesAsync();
                        success++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {dto.RowNo}: {ex.Message}");
                    }
                }

                await transaction.CommitAsync();
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Imported = success,
                        Failed = errors.Count,
                        Errors = errors
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class ImportStudentDto
    {
        public int RowNo { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RollNo { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string FatherName { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime DOB { get; set; }
    }
}