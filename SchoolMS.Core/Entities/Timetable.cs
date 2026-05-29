using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Timetable : BaseEntity
    {
        public int ClassId { get; set; }
        public Class? Class { get; set; }
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }
        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
