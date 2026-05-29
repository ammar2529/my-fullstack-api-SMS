using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    public class FeePaymentsController : BaseController<FeePayment>
    {
        private readonly AppDbContext _context;

        public FeePaymentsController(IGenericRepository<FeePayment> repo, AppDbContext context) : base(repo)
        {
            _context = context;
        }

        [HttpGet]
        public override async Task<IActionResult> GetAll()
        {
            var data = await _context.FeePayments
                .Include(p => p.Student)
                    .ThenInclude(s => s!.User)
                .Include(p => p.FeeStructure)
                    .ThenInclude(fs => fs!.Class)
                .Where(p => p.IsActive)
                .Select(p => new {
                    p.Id,
                    p.StudentId,
                    Student = p.Student != null ? new
                    {
                        p.Student.Id,
                        p.Student.RollNo,
                        FullName = p.Student.User != null ? p.Student.User.FullName : string.Empty
                    } : null,
                    p.FeeStructureId,
                    FeeStructure = p.FeeStructure != null ? new
                    {
                        p.FeeStructure.Id,
                        p.FeeStructure.FeeType,
                        p.FeeStructure.Amount,
                        Class = p.FeeStructure.Class != null ? new
                        {
                            p.FeeStructure.Class.Id,
                            p.FeeStructure.Class.ClassName,
                            p.FeeStructure.Class.Section
                        } : null
                    } : null,
                    p.AmountPaid,
                    p.PaymentDate,
                    p.Month,
                    p.PaymentMethod,
                    p.ReceivedBy,
                    p.Remarks,
                    p.Status
                })
                .ToListAsync();

            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id)
        {
            var data = await _context.FeePayments
                .Include(p => p.Student)
                    .ThenInclude(s => s!.User)
                .Include(p => p.FeeStructure)
                    .ThenInclude(fs => fs!.Class)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (data == null)
                return NotFound(new { success = false, message = "Record not found" });

            return Ok(new { success = true, data });
        }

        // Custom report ya history endpoint jo standard CRUD se hat kar hai
        [HttpGet("student-history/{studentId}")]
        public async Task<IActionResult> GetPaymentHistoryByStudent(int studentId)
        {
            var history = await _context.FeePayments
                .Include(p => p.FeeStructure)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new {
                    p.Id,
                    p.Month,
                    p.AmountPaid,
                    FeeType = p.FeeStructure!.FeeType,
                    p.PaymentDate,
                    p.PaymentMethod,
                    p.Status
                })
                .ToListAsync();

            return Ok(new { success = true, data = history });
        }

        [HttpGet("bulk-collection")]
        public async Task<IActionResult> GetBulkFeeStatus([FromQuery] int classId, [FromQuery] string month)
        {
            // 1. Pehle us class ke saare active students nikalen
            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassId == classId && s.IsActive)
                .ToListAsync();

            // 2. Is class ka fee template/structure dhoonden
            var feeStructure = await _context.FeeStructures
                .FirstOrDefaultAsync(f => f.ClassId == classId && f.IsActive);

            if (feeStructure == null)
            {
                return BadRequest(new { success = false, message = "Is class ke liye pehle Fee Structure configure karein!" });
            }

            // 3. Pehle se kiye gaye payments check karen is mahine ke
            var existingPayments = await _context.FeePayments
                .Where(p => p.Month == month && p.FeeStructureId == feeStructure.Id && p.IsActive)
                .ToDictionaryAsync(p => p.StudentId);

            // 4. Combined response taiyar karen
            var bulkData = students.Select(s => {
                var hasPaid = existingPayments.TryGetValue(s.Id, out var payment);
                return new
                {
                    StudentId = s.Id,
                    RollNo = s.RollNo,
                    FullName = s.User != null ? s.User.FullName : string.Empty,
                    FeeStructureId = feeStructure.Id,
                    TotalAmount = feeStructure.Amount, // Default structural configuration amount
                    AmountPaid = hasPaid ? payment.AmountPaid : feeStructure.Amount, // Default complete fee auto fill
                    Status = hasPaid ? payment.Status : "Unpaid", // Unpaid, Paid, ya Pending
                    PaymentMethod = hasPaid ? payment.PaymentMethod : "Cash",
                    Remarks = hasPaid ? payment.Remarks : string.Empty,
                    PaymentId = hasPaid ? (int?)payment.Id : null
                };
            }).ToList();

            return Ok(new { success = true, data = bulkData, classFee = feeStructure.Amount });
        }

        [HttpPost("bulk-save")]
        public async Task<IActionResult> SaveBulkFees([FromBody] List<BulkFeeSaveDto> feeList)
        {
            if (feeList == null || !feeList.Any())
                return BadRequest(new { success = false, message = "No data provided" });

            foreach (var item in feeList)
            {
                // Status matching rule: Agar total se kam pay kia tu auto 'Pending', barabar kia tu 'Paid'
                string finalStatus = item.AmountPaid >= item.TotalAmount ? "Paid" : (item.AmountPaid <= 0 ? "Unpaid" : "Pending");

                if (item.PaymentId.HasValue && item.PaymentId > 0)
                {
                    // Purana record update karein
                    var existing = await _context.FeePayments.FindAsync(item.PaymentId.Value);
                    if (existing != null)
                    {
                        existing.AmountPaid = item.AmountPaid;
                        existing.Status = finalStatus;
                        existing.PaymentMethod = item.PaymentMethod;
                        existing.Remarks = item.Remarks;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (item.AmountPaid > 0) // Naya transaction record sirf tab banayen jab payment receive ho
                {
                    var newPayment = new FeePayment
                    {
                        StudentId = item.StudentId,
                        FeeStructureId = item.FeeStructureId,
                        AmountPaid = item.AmountPaid,
                        Month = item.Month,
                        PaymentDate = DateTime.UtcNow,
                        PaymentMethod = item.PaymentMethod,
                        Status = finalStatus,
                        Remarks = item.Remarks,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.FeePayments.AddAsync(newPayment);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Fees collection updated successfully!" });
        }

        // Request DTO class controller file ke baahir ya bottom par rakh sakte hain
        public class BulkFeeSaveDto
        {
            public int? PaymentId { get; set; }
            public int StudentId { get; set; }
            public int FeeStructureId { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal AmountPaid { get; set; }
            public string Month { get; set; } = string.Empty;
            public string PaymentMethod { get; set; } = "Cash";
            public string? Remarks { get; set; }
        }
    }
}
