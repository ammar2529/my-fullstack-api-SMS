using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class ExamResult : BaseEntity
    {
        public int ExamId { get; set; }
        public Exam? Exam { get; set; }
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }
        public decimal ObtainedMarks { get; set; }
        public string Grade { get; set; } = string.Empty;
    }
}
