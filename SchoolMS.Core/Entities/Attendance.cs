using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Attendance : BaseEntity
    {
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public int ClassId { get; set; }
        public Class? Class { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; } = string.Empty; // Present, Absent, Leave
        public int MarkedBy { get; set; }
    }
}
