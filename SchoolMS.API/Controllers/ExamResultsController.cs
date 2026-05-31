using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolMS.API.Extensions;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExamResultsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ExamResultsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var results = await _context.ExamResults
                .Include(r => r.Exam)
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Subject)
                .Select(r => new {
                    r.Id,
                    r.ExamId,
                    ExamName = r.Exam!.ExamName,
                    r.StudentId,
                    StudentName = r.Student!.User!.FullName,
                    RollNo = r.Student!.RollNo,
                    r.SubjectId,
                    SubjectName = r.Subject!.SubjectName,
                    r.ObtainedMarks,
                    TotalMarks = r.Exam!.TotalMarks,
                    r.Grade,
                    r.IsActive
                })
                .ToListAsync();
            return Ok(new { success = true, data = results });
        }

        [HttpGet("byexam/{examId}")]
        public async Task<IActionResult> GetByExam(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
                return NotFound(new { success = false, message = "Exam not found" });

            // Sab students of this class
            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassId == exam.ClassId)
                .ToListAsync();

            // Subjects of this class
            var subjects = await _context.Subjects
                .Where(s => s.ClassId == exam.ClassId)
                .ToListAsync();

            // Existing results
            var existing = await _context.ExamResults
                .Where(r => r.ExamId == examId)
                .ToListAsync();

            var result = new
            {
                Exam = new
                {
                    exam.Id,
                    exam.ExamName,
                    exam.TotalMarks,
                    ClassName = exam.Class!.ClassName + " - " + exam.Class.Section
                },
                Students = students.Select(s => new {
                    s.Id,
                    s.RollNo,
                    FullName = s.User!.FullName
                }),
                Subjects = subjects.Select(s => new { s.Id, s.SubjectName }),
                Results = existing.Select(r => new {
                    r.Id,
                    r.StudentId,
                    r.SubjectId,
                    r.ObtainedMarks,
                    r.Grade
                })
            };

            return Ok(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ExamResult dto)
        {
            var exists = await _context.ExamResults.AnyAsync(r =>
                r.ExamId == dto.ExamId &&
                r.StudentId == dto.StudentId &&
                r.SubjectId == dto.SubjectId);

            if (exists)
                return BadRequest(new { success = false, message = "Result already exists" });

            // Auto grade
            var exam = await _context.Exams.FindAsync(dto.ExamId);
            if (exam != null)
                dto.Grade = CalculateGrade(dto.ObtainedMarks, exam.TotalMarks);

            dto.IsActive = true;
            dto.CreatedAt = DateTime.UtcNow;
            _context.ExamResults.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = dto.Id });
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkSave([FromBody] List<ExamResult> list)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var dto in list)
                {
                    var exam = await _context.Exams.FindAsync(dto.ExamId);
                    if (exam != null)
                        dto.Grade = CalculateGrade(dto.ObtainedMarks, exam.TotalMarks);

                    var existing = await _context.ExamResults.FirstOrDefaultAsync(r =>
                        r.ExamId == dto.ExamId &&
                        r.StudentId == dto.StudentId &&
                        r.SubjectId == dto.SubjectId);

                    if (existing != null)
                    {
                        existing.ObtainedMarks = dto.ObtainedMarks;
                        existing.Grade = dto.Grade;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        dto.IsActive = true;
                        dto.CreatedAt = DateTime.UtcNow;
                        _context.ExamResults.Add(dto);
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Results saved successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _context.ExamResults.FindAsync(id);
            if (result == null)
                return NotFound(new { success = false, message = "Not found" });

            result.IsActive = false;
            result.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted successfully" });
        }

        private string CalculateGrade(decimal obtained, decimal total)
        {
            var percentage = (obtained / total) * 100;
            return percentage switch
            {
                >= 90 => "A+",
                >= 80 => "A",
                >= 70 => "B",
                >= 60 => "C",
                >= 50 => "D",
                _ => "F"
            };
        }

        [HttpGet("byexam/{examId}/{classId}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetByExamAndClass(int examId, int classId)
        {

            var userId = User.GetUserId();
            var role = User.GetRole();

            // Teacher check — sirf apni assigned class
            if (role == "Teacher")
            {
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher != null)
                {
                    var isAssigned = await _context.TeacherClasses
                        .AnyAsync(tc => tc.TeacherId == teacher.Id
                                     && tc.ClassId == classId
                                     && tc.IsActive);

                    if (!isAssigned)
                        return Forbid();
                }
            }
            var exam = await _context.Exams
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
                return NotFound(new { success = false, message = "Exam not found" });

            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassId == classId)
                .Select(s => new { s.Id, s.RollNo, FullName = s.User!.FullName })
                .ToListAsync();

            var subjects = await _context.Subjects
                .Where(s => s.ClassId == classId)
                .Select(s => new {
                    s.Id,
                    s.SubjectName,
                    // PerSubject ho tu subject ki marks, warna exam ki marks
                    s.TotalMarks
                })
                .ToListAsync();

            var existing = await _context.ExamResults
                .Where(r => r.ExamId == examId)
                .Select(r => new { r.StudentId, r.SubjectId, r.ObtainedMarks, r.Grade, r.Id })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    exam = new
                    {
                        exam.Id,
                        exam.ExamName,
                        exam.TotalMarks,
                        ClassName = exam.Class!.ClassName + " - " + exam.Class.Section
                    },
                    students,
                    subjects,
                    results = existing
                }
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetResultsList()
        {
            var results = await _context.ExamResults
                .Include(r => r.Exam).ThenInclude(e => e!.Class)
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Subject)
                .Select(r => new {
                    r.Id,
                    r.ExamId,
                    ExamName = r.Exam!.ExamName,
                    r.StudentId,
                    StudentName = r.Student!.User!.FullName,
                    RollNo = r.Student!.RollNo,
                    ClassId = r.Exam!.ClassId,
                    ClassName = r.Exam!.Class!.ClassName + " - " + r.Exam!.Class.Section,
                    r.SubjectId,
                    SubjectName = r.Subject!.SubjectName,
                    r.ObtainedMarks,
                    TotalMarks = r.Exam!.TotalMarks,
                    r.Grade
                })
                .ToListAsync();
            return Ok(new { success = true, data = results });
        }

        [HttpGet("reportcard/{studentId}/{examId}")]
        public async Task<IActionResult> GetReportCard(int studentId, int examId)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            var exam = await _context.Exams.FindAsync(examId);

            var results = await _context.ExamResults
                .Include(r => r.Subject)
                .Include(r => r.Exam)
                .Where(r => r.StudentId == studentId && r.ExamId == examId)
                .Select(r => new {
                    r.SubjectId,
                    SubjectName = r.Subject!.SubjectName,
                    r.ObtainedMarks,
                    TotalMarks = r.Exam!.TotalMarks,
                    r.Grade,
                    Percentage = Math.Round((r.ObtainedMarks / r.Exam!.TotalMarks) * 100, 1)
                })
                .ToListAsync();

            var totalObtained = results.Sum(r => r.ObtainedMarks);
            var totalMarks = results.Sum(r => r.TotalMarks);
            var percentage = totalMarks > 0
                ? Math.Round((totalObtained / totalMarks) * 100, 1) : 0;

            return Ok(new
            {
                success = true,
                data = new
                {
                    Student = new
                    {
                        student.Id,
                        student.RollNo,
                        FullName = student.User!.FullName,
                        ClassName = student.Class!.ClassName + " - " + student.Class.Section,
                    },
                    Exam = new { exam!.Id, exam.ExamName, exam.ExamDate },
                    Results = results,
                    Summary = new
                    {
                        TotalObtained = totalObtained,
                        TotalMarks = totalMarks,
                        Percentage = percentage,
                        Grade = CalculateGrade(totalObtained, totalMarks)
                    }
                }
            });
        }
    }
}