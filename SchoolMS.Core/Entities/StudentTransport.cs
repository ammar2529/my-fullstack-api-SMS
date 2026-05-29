using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class StudentTransport : BaseEntity
    {
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public int TransportId { get; set; }
        public Transport? Transport { get; set; }
        public string PickupPoint { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}
