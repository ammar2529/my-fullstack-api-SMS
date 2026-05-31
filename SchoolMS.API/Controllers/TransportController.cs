using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class TransportController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TransportController(AppDbContext context) => _context = context;

        // =============================================
        // ROUTES
        // =============================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var routes = await _context.Transports
                .Select(t => new {
                    t.Id,
                    t.RouteName,
                    t.DriverName,
                    t.VehicleNo,
                    t.Capacity,
                    t.IsActive,
                    AssignedStudents = _context.StudentTransports
                        .Count(st => st.TransportId == t.Id)
                })
                .ToListAsync();
            return Ok(new { success = true, data = routes });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransportDto dto)
        {
            var transport = new Transport
            {
                RouteName = dto.RouteName,
                DriverName = dto.DriverName,
                VehicleNo = dto.VehicleNo,
                Capacity = dto.Capacity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Transports.Add(transport);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = transport.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateTransportDto dto)
        {
            var transport = await _context.Transports.FindAsync(id);
            if (transport == null)
                return NotFound(new { success = false, message = "Not found" });

            transport.RouteName = dto.RouteName;
            transport.DriverName = dto.DriverName;
            transport.VehicleNo = dto.VehicleNo;
            transport.Capacity = dto.Capacity;
            transport.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var transport = await _context.Transports.FindAsync(id);
            if (transport == null)
                return NotFound(new { success = false, message = "Not found" });

            transport.IsActive = false;
            transport.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }

        // =============================================
        // STUDENT TRANSPORT ASSIGNMENTS
        // =============================================
        [HttpGet("students")]
        public async Task<IActionResult> GetStudentTransports()
        {
            var list = await _context.StudentTransports
                .Include(st => st.Student).ThenInclude(s => s!.User)
                .Include(st => st.Transport)
                .Select(st => new {
                    st.Id,
                    st.StudentId,
                    StudentName = st.Student!.User!.FullName,
                    RollNo = st.Student!.RollNo,
                    st.TransportId,
                    RouteName = st.Transport!.RouteName,
                    st.PickupPoint,
                    st.AssignedDate,
                    st.IsActive
                })
                .ToListAsync();
            return Ok(new { success = true, data = list });
        }

        [HttpPost("students")]
        public async Task<IActionResult> AssignStudent([FromBody] CreateStudentTransportDto dto)
        {
            var exists = await _context.StudentTransports.AnyAsync(st =>
                st.StudentId == dto.StudentId && st.IsActive);

            if (exists)
                return BadRequest(new { success = false, message = "Student already assigned to a route!" });

            var transport = await _context.Transports.FindAsync(dto.TransportId);
            if (transport == null)
                return NotFound(new { success = false, message = "Route not found" });

            var assigned = await _context.StudentTransports
                .CountAsync(st => st.TransportId == dto.TransportId && st.IsActive);

            if (assigned >= transport.Capacity)
                return BadRequest(new { success = false, message = "Vehicle is at full capacity!" });

            var st2 = new StudentTransport
            {
                StudentId = dto.StudentId,
                TransportId = dto.TransportId,
                PickupPoint = dto.PickupPoint,
                AssignedDate = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.StudentTransports.Add(st2);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = st2.Id });
        }

        [HttpDelete("students/{id}")]
        public async Task<IActionResult> RemoveStudent(int id)
        {
            var st = await _context.StudentTransports.FindAsync(id);
            if (st == null)
                return NotFound(new { success = false, message = "Not found" });

            st.IsActive = false;
            st.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Removed successfully" });
        }
    }

    public class CreateTransportDto
    {
        public string RouteName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }

    public class CreateStudentTransportDto
    {
        public int StudentId { get; set; }
        public int TransportId { get; set; }
        public string PickupPoint { get; set; } = string.Empty;
    }
}