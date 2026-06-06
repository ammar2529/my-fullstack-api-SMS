using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            int? userId, string userName,
            string action, string entity,
            string details, string ipAddress = "")
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Entity = entity,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}