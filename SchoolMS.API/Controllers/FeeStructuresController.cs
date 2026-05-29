using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    public class FeeStructuresController : BaseController<FeeStructure>
    {
        private readonly AppDbContext _context;

        public FeeStructuresController(IGenericRepository<FeeStructure> repo, AppDbContext context) : base(repo)
        {
            _context = context;
        }

        [HttpGet]
        public override async Task<IActionResult> GetAll()
        {
            var data = await _context.FeeStructures
                .Include(f => f.Class)
                .Where(f => f.IsActive)
                .Select(f => new
                {
                    f.Id,
                    f.ClassId,
                    Class = f.Class != null ? new
                    {
                        f.Class.Id,
                        f.Class.ClassName,
                        f.Class.Section
                    } : null,
                    f.FeeType,
                    f.Amount,
                    f.CreatedAt,
                    f.UpdatedAt,
                    f.IsActive
                })
                .ToListAsync();

            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id)
        {
            var data = await _context.FeeStructures
                .Include(f => f.Class)
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

            if (data == null)
                return NotFound(new { success = false, message = "Record not found" });

            return Ok(new { success = true, data });
        }
    }
}
