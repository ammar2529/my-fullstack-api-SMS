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
    public class NoticesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public NoticesController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var notices = await _context.Notices
                .OrderByDescending(n => n.NoticeDate)
                .Select(n => new {
                    n.Id,
                    n.Title,
                    n.Description,
                    n.NoticeDate,
                    n.CreatedBy,
                    n.IsActive
                })
                .ToListAsync();
            return Ok(new { success = true, data = notices });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNoticeDto dto)
        {
            var notice = new Notice
            {
                Title = dto.Title,
                Description = dto.Description,
                NoticeDate = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notices.Add(notice);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = notice.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateNoticeDto dto)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice == null)
                return NotFound(new { success = false, message = "Notice not found" });

            notice.Title = dto.Title;
            notice.Description = dto.Description;
            notice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice == null)
                return NotFound(new { success = false, message = "Notice not found" });

            notice.IsActive = false;
            notice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }
    }

    public class CreateNoticeDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
    }
}