using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class FeeStructure : BaseEntity
    {
        public int ClassId { get; set; }
        public Class? Class { get; set; }
        public string FeeType { get; set; } = string.Empty;
        public string FeeCategory { get; set; } = "Monthly"; // Monthly, OneTime, Optional
        public decimal Amount { get; set; }
        public bool IsOptional { get; set; } = false;
        public string Description { get; set; } = string.Empty;
    }
}
