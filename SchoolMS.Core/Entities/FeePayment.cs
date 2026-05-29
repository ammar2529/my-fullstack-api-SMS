using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Entities
{
    public class FeePayment : BaseEntity
    {
        public int StudentId { get; set; }
        public Student? Student { get; set; }
        public int FeeStructureId { get; set; }
        public FeeStructure? FeeStructure { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string Month { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Bank
        public int ReceivedBy { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Status { get; set; } = "Paid"; // Paid, Partial, Pending
    }
}
