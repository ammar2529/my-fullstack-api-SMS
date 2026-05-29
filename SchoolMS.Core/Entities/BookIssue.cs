using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class BookIssue : BaseEntity
    {
        public int BookId { get; set; }
        public Book? Book { get; set; }
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public decimal Fine { get; set; } = 0;
        public string Status { get; set; } = "Issued"; // Issued, Returned
    }
}
