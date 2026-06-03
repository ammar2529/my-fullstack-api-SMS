using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Holiday : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string HolidayType { get; set; } = "Public";
    }
}
