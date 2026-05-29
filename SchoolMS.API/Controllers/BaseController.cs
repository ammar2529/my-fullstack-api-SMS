using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseController<T> : ControllerBase where T : BaseEntity
    {
        private readonly IGenericRepository<T> _repo;

        public BaseController(IGenericRepository<T> repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetAll()
        {
            var data = await _repo.GetAllAsync();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(int id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Record not found" });
            return Ok(new { success = true, data });
        }

        [HttpPost]
        public async Task<IActionResult> Create(T entity)
        {
            var result = await _repo.AddAsync(entity);
            return Ok(new { success = true, data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, T entity)
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(entity);
            return Ok(new { success = true, message = "Updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return Ok(new { success = true, message = "Deleted successfully" });
        }
    }
}