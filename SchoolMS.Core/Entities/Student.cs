using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Student : BaseEntity
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public string RollNo { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public Class? Class { get; set; }
        public string FatherName { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;
    }
}
