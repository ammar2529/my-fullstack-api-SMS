using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Datesheet : BaseEntity
    {
        public string ExamTitle { get; set; } = string.Empty; // Mid Term, Final Term
        public int ClassId { get; set; }
        public Class? Class { get; set; }
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
