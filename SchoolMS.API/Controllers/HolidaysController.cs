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
    public class HolidaysController : ControllerBase
    {
        private readonly AppDbContext _context;
        public HolidaysController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var holidays = await _context.Holidays
                .OrderBy(h => h.StartDate)
                .Select(h => new {
                    h.Id,
                    h.Title,
                    h.Description,
                    h.StartDate,
                    h.EndDate,
                    h.HolidayType,
                    h.IsActive,
                    h.CreatedAt
                })
                .ToListAsync();
            return Ok(new { success = true, data = holidays });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateHolidayDto dto)
        {
            var userId = User.GetUserId();
            var holiday = new Holiday
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                HolidayType = dto.HolidayType,
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = holiday.Id });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateHolidayDto dto)
        {
            var userId = User.GetUserId();
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
                return NotFound(new { success = false, message = "Not found" });

            holiday.Title = dto.Title;
            holiday.Description = dto.Description;
            holiday.StartDate = dto.StartDate;
            holiday.EndDate = dto.EndDate;
            holiday.HolidayType = dto.HolidayType;
            holiday.UpdatedBy = userId;
            holiday.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
                return NotFound(new { success = false, message = "Not found" });

            holiday.IsActive = false;
            holiday.UpdatedBy = userId;
            holiday.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted" });
        }
    }

    public class CreateHolidayDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string HolidayType { get; set; } = "Public"; // Public, School, Exam
    }
}