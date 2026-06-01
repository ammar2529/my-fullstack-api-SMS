using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class SchoolSettings : BaseEntity
    {
        public string SchoolName { get; set; } = string.Empty;
        public string SchoolAddress { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Principal { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
