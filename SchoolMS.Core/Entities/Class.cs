using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Class : BaseEntity
    {
        public string ClassName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}