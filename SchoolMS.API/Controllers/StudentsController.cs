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

            // 1. Base Query build karein
            IQueryable<Student> query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Include(s => s.Country); // Country include kiya list ke liye bhi

            // Teacher sirf apni class ke students dekhe (Filter Logic)
            if (role == "Teacher")
            {
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher == null)
                    return Ok(new { success = true, data = new List<StudentResponseDto>() });

                var teacherClassIds = await _context.TeacherClasses
                    .Where(tc => tc.TeacherId == teacher.Id && tc.IsActive)
                    .Select(tc => tc.ClassId)
                    .ToListAsync();

                query = query.Where(s => teacherClassIds.Contains(s.ClassId));
            }

            // 2. Query variable par .Select() laga kar data ko DTO mein map karein
            var students = await query
                .Select(s => new StudentResponseDto
                {
                    Id = s.Id,
                    RollNo = s.RollNo,
                    AdmissionNo = s.AdmissionNo,
                    FullName = s.User!.FullName,
                    Email = s.User!.Email,
                    ClassId = s.ClassId, // 👈 Yeh missing tha, list data se edit dropdown select karne ke liye lazmi hai
                    ClassName = s.Class!.ClassName,
                    Section = s.Class!.Section,
                    FatherName = s.FatherName,
                    PhoneNo = s.PhoneNo,
                    Address = s.Address,
                    DOB = s.DOB,
                    AdmissionDate = s.AdmissionDate,
                    IsActive = s.IsActive,
                    ProfilePicture = string.IsNullOrEmpty(s.ProfilePicture)
                        ? null
                        : (s.ProfilePicture.StartsWith("/uploads/") ? s.ProfilePicture : $"/uploads/students/{s.ProfilePicture}"),
                    CountryId = s.CountryId,
                    CountryName = s.Country != null ? s.Country.Name : ""
                })
                .ToListAsync();

            return Ok(new { success = true, data = students });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // 1. Student ko uski related tables ke sath include karke fetch karein
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Include(s => s.Country)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            // 2. Data ko usi same DTO mein map karein jo GetAll mein use kiya tha
            var studentDto = new StudentResponseDto
            {
                Id = student.Id,
                RollNo = student.RollNo,
                AdmissionNo = student.AdmissionNo,
                FullName = student.User!.FullName,
                Email = student.User!.Email,
                ClassId = student.ClassId,
                ClassName = student.Class!.ClassName,
                Section = student.Class!.Section,
                CountryId = student.CountryId,
                CountryName = student.Country?.Name,
                FatherName = student.FatherName,
                PhoneNo = student.PhoneNo,
                Address = student.Address,
                DOB = student.DOB,
                AdmissionDate = student.AdmissionDate,
                IsActive = student.IsActive,
                ProfilePicture = string.IsNullOrEmpty(student.ProfilePicture)
                    ? null
                    : (student.ProfilePicture.StartsWith("/uploads/") ? student.ProfilePicture : $"/uploads/students/{student.ProfilePicture}")
            };

            return Ok(new { success = true, data = studentDto });
        } // 👈 Bracket lagakar method yahan cross-close kar diya

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateStudentDto dto)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1";
            int.TryParse(userIdStr, out int userId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentDate = DateTime.Now;
                string yearPart = currentDate.ToString("yy");
                string monthPart = currentDate.ToString("MMM").ToUpper();
                string currentMonthPrefix = $"ADM-{yearPart}-{monthPart}-";

                var lastStudentThisMonth = await _context.Students
                    .Where(s => s.AdmissionNo.StartsWith(currentMonthPrefix))
                    .OrderByDescending(s => s.Id)
                    .FirstOrDefaultAsync();

                string nextAdmissionNo = $"{currentMonthPrefix}0001";

                if (lastStudentThisMonth != null)
                {
                    string numericPart = lastStudentThisMonth.AdmissionNo.Substring(lastStudentThisMonth.AdmissionNo.Length - 4);
                    if (int.TryParse(numericPart, out int lastSequence))
                    {
                        nextAdmissionNo = currentMonthPrefix + (lastSequence + 1).ToString("D4");
                    }
                }

                var lastAnyStudent = await _context.Students
                    .OrderByDescending(s => s.Id)
                    .FirstOrDefaultAsync();

                string nextRollNo = "1001";
                if (lastAnyStudent != null && int.TryParse(lastAnyStudent.RollNo, out int lastRollInt))
                {
                    nextRollNo = (lastRollInt + 1).ToString();
                }

                var user = new User
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? "Student@123"),
                    RoleId = 3,
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                string? fileName = null;
                if (dto.ImageFile != null && dto.ImageFile.Length > 0)
                {
                    var uploadsFolder = @"D:\SMS\Student";
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.ImageFile.CopyToAsync(fileStream);
                    }
                }

                var student = new Student
                {
                    UserId = user.Id,
                    RollNo = nextRollNo,
                    AdmissionNo = nextAdmissionNo,
                    ClassId = dto.ClassId,
                    CountryId = dto.CountryId,
                    FatherName = dto.FatherName,
                    PhoneNo = dto.PhoneNo,
                    Address = dto.Address,
                    DOB = dto.DOB,
                    AdmissionDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    ProfilePicture = fileName != null ? $"/uploads/students/{fileName}" : null
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { success = true, data = student.Id, message = $"Student created! RollNo: {nextRollNo}, AdmNo: {nextAdmissionNo}" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateStudentDto dto)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1";
            int.TryParse(userIdStr, out int userId);

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                var uploadsFolder = @"D:\SMS\Student";
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (!string.IsNullOrEmpty(student.ProfilePicture))
                {
                    var oldFileName = Path.GetFileName(student.ProfilePicture);
                    var oldFilePath = Path.Combine(uploadsFolder, oldFileName);

                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                var newFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.ImageFile.FileName);
                var newFilePath = Path.Combine(uploadsFolder, newFileName);

                using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(fileStream);
                }

                student.ProfilePicture = $"/uploads/students/{newFileName}";
            }

            student.ClassId = dto.ClassId;
            student.CountryId = dto.CountryId;
            student.FatherName = dto.FatherName;
            student.PhoneNo = dto.PhoneNo;
            student.Address = dto.Address;
            student.DOB = dto.DOB;
            student.UpdatedBy = userId;
            student.UpdatedAt = DateTime.UtcNow;

            if (student.User != null)
            {
                student.User.FullName = dto.FullName;
                student.User.Email = dto.Email;
                student.User.UpdatedBy = userId;
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

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] int? excludeStudentId)
        {
            bool exists = await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != excludeStudentId);
            return Ok(new { isDuplicate = exists });
        }
    } // 👈 Controller closing class bracket

    // ==========================================
    // DTOs SECTION (Outside Controller Class)
    // ==========================================

    public class StudentResponseDto
    {
        public int Id { get; set; }
        public string RollNo { get; set; } = null!;
        public string AdmissionNo { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string Section { get; set; } = null!;
        public string FatherName { get; set; } = null!;
        public string PhoneNo { get; set; } = null!;
        public string Address { get; set; } = null!;
        public DateTime DOB { get; set; }
        public DateTime AdmissionDate { get; set; }
        public bool IsActive { get; set; }
        public string? ProfilePicture { get; set; }
        public int? CountryId { get; set; }
        public string? CountryName { get; set; }
    }

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
        public IFormFile? ImageFile { get; set; }
        public int? CountryId { get; set; }
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
        public IFormFile? ImageFile { get; set; }
        public int? CountryId { get; set; }
    }
}