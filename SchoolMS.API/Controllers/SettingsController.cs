using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;
using SchoolMS.API.Extensions;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SettingsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var settings = await _context.SchoolSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                // Default settings return karo
                return Ok(new
                {
                    success = true,
                    data = new SchoolSettings
                    {
                        SchoolName = "School Management System",
                        AcademicYear = DateTime.Now.Year.ToString()
                    }
                });
            }
            return Ok(new { success = true, data = settings });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Save([FromBody] SchoolSettings dto)
        {
            var userId = User.GetUserId();
            var existing = await _context.SchoolSettings.FirstOrDefaultAsync();

            if (existing == null)
            {
                dto.CreatedBy = userId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.IsActive = true;
                _context.SchoolSettings.Add(dto);
            }
            else
            {
                existing.SchoolName = dto.SchoolName;
                existing.SchoolAddress = dto.SchoolAddress;
                existing.PhoneNo = dto.PhoneNo;
                existing.Email = dto.Email;
                existing.Website = dto.Website;
                existing.Principal = dto.Principal;
                existing.AcademicYear = dto.AcademicYear;
                existing.City = dto.City;
                existing.Country = dto.Country;
                existing.UpdatedBy = userId;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Settings saved successfully!" });
        }
    }
}