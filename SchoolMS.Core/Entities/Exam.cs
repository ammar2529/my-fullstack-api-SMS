using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Exam : BaseEntity
    {
        public string ExamName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public Class? Class { get; set; }
        public DateTime ExamDate { get; set; }
        public decimal TotalMarks { get; set; }
    }
}
