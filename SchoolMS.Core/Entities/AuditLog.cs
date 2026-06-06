using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // Create, Update, Delete
        public string Entity { get; set; } = string.Empty; // Student, Teacher etc
        public string Details { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
