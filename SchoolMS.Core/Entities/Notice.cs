using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class Notice : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime NoticeDate { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
    }
}
