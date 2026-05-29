using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Teacher : BaseEntity
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; } = DateTime.UtcNow;
        public decimal Salary { get; set; }
    }
}
