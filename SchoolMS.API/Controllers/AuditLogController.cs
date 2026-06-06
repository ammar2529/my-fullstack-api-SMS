using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AuditLogController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int size = 50,
            [FromQuery] string? action = null,
            [FromQuery] string? entity = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);
            if (!string.IsNullOrEmpty(entity))
                query = query.Where(a => a.Entity == entity);

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return Ok(new { success = true, data = logs, total, page, size });
        }
    }
}