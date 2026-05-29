using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Subject : BaseEntity
    {
        public string SubjectName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public Class? Class { get; set; }
        public decimal TotalMarks { get; set; } = 100; // Per subject marks

    }
}
