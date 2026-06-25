using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CountriesController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var countries = await _context.Countries
                        .OrderBy(c => c.Name)
                        .ToListAsync<Country>();

            return Ok(new { success = true, data = countries });
        }
    }
    }

