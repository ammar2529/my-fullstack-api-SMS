using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClassesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClassesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var classes = await _context.Classes
                .Select(c => new { c.Id, c.ClassName, c.Section, c.IsActive })
                .ToListAsync();

            return Ok(new { success = true, data = classes });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Class dto)
        {
            dto.IsActive = true;
            dto.CreatedAt = DateTime.UtcNow;
            _context.Classes.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = dto.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Class dto)
        {
            var cls = await _context.Classes.FindAsync(id);
            if (cls == null)
                return NotFound(new { success = false, message = "Class not found" });

            cls.ClassName = dto.ClassName;
            cls.Section = dto.Section;
            cls.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cls = await _context.Classes.FindAsync(id);
            if (cls == null)
                return NotFound(new { success = false, message = "Class not found" });

            cls.IsActive = false;
            cls.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }
    }
}

