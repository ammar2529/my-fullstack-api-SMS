using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LibraryController : ControllerBase
    {
        private readonly AppDbContext _context;
        public LibraryController(AppDbContext context) => _context = context;

        // =============================================
        // BOOKS
        // =============================================
        [HttpGet("books")]
        public async Task<IActionResult> GetBooks()
        {
            var books = await _context.Books
                .Select(b => new {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.ISBN,
                    b.TotalCopies,
                    b.Available,
                    b.IsActive,
                    IssuedCopies = b.TotalCopies - b.Available
                })
                .ToListAsync();
            return Ok(new { success = true, data = books });
        }

        [HttpPost("books")]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookDto dto)
        {
            var book = new Book
            {
                Title = dto.Title,
                Author = dto.Author,
                ISBN = dto.ISBN,
                TotalCopies = dto.TotalCopies,
                Available = dto.TotalCopies,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = book.Id });
        }

        [HttpPut("books/{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] CreateBookDto dto)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound(new { success = false, message = "Book not found" });

            book.Title = dto.Title;
            book.Author = dto.Author;
            book.ISBN = dto.ISBN;
            book.TotalCopies = dto.TotalCopies;
            book.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated successfully" });
        }

        [HttpDelete("books/{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound(new { success = false, message = "Book not found" });

            book.IsActive = false;
            book.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }

        // =============================================
        // BOOK ISSUES
        // =============================================
        [HttpGet("issues")]
        public async Task<IActionResult> GetIssues()
        {
            var issues = await _context.BookIssues
                .Include(i => i.Book)
                .Include(i => i.Student).ThenInclude(s => s!.User)
                .Select(i => new {
                    i.Id,
                    i.BookId,
                    BookTitle = i.Book!.Title,
                    i.StudentId,
                    StudentName = i.Student!.User!.FullName,
                    RollNo = i.Student!.RollNo,
                    i.IssueDate,
                    i.DueDate,
                    i.ReturnDate,
                    i.Fine,
                    i.Status,
                    i.IsActive,
                    IsOverdue = i.ReturnDate == null && i.DueDate < DateTime.UtcNow
                })
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
            return Ok(new { success = true, data = issues });
        }

        [HttpPost("issues")]
        public async Task<IActionResult> IssueBook([FromBody] CreateIssueDto dto)
        {
            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
                return NotFound(new { success = false, message = "Book not found" });

            if (book.Available <= 0)
                return BadRequest(new { success = false, message = "No copies available!" });

            // Check student already has this book
            var alreadyIssued = await _context.BookIssues.AnyAsync(i =>
                i.StudentId == dto.StudentId &&
                i.BookId == dto.BookId &&
                i.Status == "Issued");

            if (alreadyIssued)
                return BadRequest(new { success = false, message = "Student already has this book!" });

            var issue = new BookIssue
            {
                BookId = dto.BookId,
                StudentId = dto.StudentId,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(dto.DueDays),
                Status = "Issued",
                Fine = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            book.Available--;

            _context.BookIssues.Add(issue);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = issue.Id });
        }

        [HttpPut("issues/return/{id}")]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var issue = await _context.BookIssues
                .Include(i => i.Book)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
                return NotFound(new { success = false, message = "Issue not found" });

            if (issue.Status == "Returned")
                return BadRequest(new { success = false, message = "Already returned!" });

            // Calculate fine
            var returnDate = DateTime.UtcNow;
            decimal fine = 0;

            if (returnDate > issue.DueDate)
            {
                var overdueDays = (returnDate - issue.DueDate).Days;
                fine = overdueDays * 10; // Rs 10 per day
            }

            issue.ReturnDate = returnDate;
            issue.Status = "Returned";
            issue.Fine = fine;
            issue.UpdatedAt = DateTime.UtcNow;

            // Increase available copies
            if (issue.Book != null)
                issue.Book.Available++;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = new { fine, message = fine > 0 ? $"Fine: Rs {fine}" : "Returned successfully" } });
        }

        [HttpDelete("issues/{id}")]
        public async Task<IActionResult> DeleteIssue(int id)
        {
            var issue = await _context.BookIssues.FindAsync(id);
            if (issue == null)
                return NotFound(new { success = false, message = "Not found" });

            issue.IsActive = false;
            issue.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }
    }

    public class CreateBookDto
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public int TotalCopies { get; set; } = 1;
    }

    public class CreateIssueDto
    {
        public int BookId { get; set; }
        public int StudentId { get; set; }
        public int DueDays { get; set; } = 14;
    }
}