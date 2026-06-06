using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;
using SchoolMS.API.Extensions;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    public class FeeStructuresController : BaseController<FeeStructure>
    {
        private readonly AppDbContext _context;

        public FeeStructuresController(
            IGenericRepository<FeeStructure> repo,
            AppDbContext context) : base(repo)
        {
            _context = context;
        }

        [HttpGet]
        public override async Task<IActionResult> GetAll()
        {
            var data = await _context.FeeStructures
                .Include(f => f.Class)
                .Where(f => f.IsActive)
                .Select(f => new {
                    f.Id,
                    f.ClassId,
                    ClassName = f.Class != null ? f.Class.ClassName + " - " + f.Class.Section : "",
                    f.FeeType,
                    f.FeeCategory,
                    f.Amount,
                    f.IsOptional,
                    f.Description,
                    f.IsActive
                })
                .OrderBy(f => f.ClassName)
                .ThenBy(f => f.FeeCategory)
                .ToListAsync();

            return Ok(new { success = true, data });
        }

        [HttpGet("byclass/{classId}")]
        public async Task<IActionResult> GetByClass(int classId)
        {
            var data = await _context.FeeStructures
                .Where(f => f.ClassId == classId && f.IsActive)
                .Select(f => new {
                    f.Id,
                    f.ClassId,
                    f.FeeType,
                    f.FeeCategory,
                    f.Amount,
                    f.IsOptional,
                    f.Description
                })
                .ToListAsync();
            return Ok(new { success = true, data });
        }

        [HttpPost("create-fee")]
        [Authorize(Roles = "Admin")]
        public  async Task<IActionResult> CreateFee([FromBody] CreateFeeDto dto)
        {
            var userId = User.GetUserId();
            var fee = new FeeStructure
            {
                ClassId = dto.ClassId,
                FeeType = dto.FeeType,
                FeeCategory = dto.FeeCategory,
                Amount = dto.Amount,
                IsOptional = dto.IsOptional,
                Description = dto.Description,
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.FeeStructures.Add(fee);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = fee.Id });
        }

        [HttpPut("update-fee/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFee(int id, [FromBody] CreateFeeDto dto)
        {
            var userId = User.GetUserId();
            var fee = await _context.FeeStructures.FindAsync(id);
            if (fee == null)
                return NotFound(new { success = false, message = "Not found" });

            fee.ClassId = dto.ClassId;
            fee.FeeType = dto.FeeType;
            fee.FeeCategory = dto.FeeCategory;
            fee.Amount = dto.Amount;
            fee.IsOptional = dto.IsOptional;
            fee.Description = dto.Description;
            fee.UpdatedBy = userId;
            fee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated" });
        }

        [HttpDelete("delete-fee/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFee(int id)
        {
            var userId = User.GetUserId();
            var fee = await _context.FeeStructures.FindAsync(id);
            if (fee == null)
                return NotFound(new { success = false, message = "Not found" });
            fee.IsActive = false;
            fee.UpdatedBy = userId;
            fee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted" });
        }
    }

    public class CreateFeeDto
    {
        public int ClassId { get; set; }
        public string FeeType { get; set; } = string.Empty;
        public string FeeCategory { get; set; } = "Monthly";
        public decimal Amount { get; set; }
        public bool IsOptional { get; set; } = false;
        public string Description { get; set; } = string.Empty;
    }
}