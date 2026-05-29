using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public Role? Role { get; set; }
    }
}
