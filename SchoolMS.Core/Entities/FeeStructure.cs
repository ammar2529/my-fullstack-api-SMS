using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class FeeStructure : BaseEntity
    {
        public int ClassId { get; set; }
        public Class? Class { get; set; }
        public string FeeType { get; set; } = string.Empty; // Monthly, Admission, Exam
        public decimal Amount { get; set; }
    }
}
