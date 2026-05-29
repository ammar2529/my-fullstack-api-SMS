using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;

namespace SchoolMS.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamResult> ExamResults => Set<ExamResult>();
        public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
        public DbSet<FeePayment> FeePayments => Set<FeePayment>();
        public DbSet<Timetable> Timetables => Set<Timetable>();
        public DbSet<Notice> Notices => Set<Notice>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<BookIssue> BookIssues => Set<BookIssue>();
        public DbSet<Transport> Transports => Set<Transport>();
        public DbSet<StudentTransport> StudentTransports => Set<StudentTransport>();
        public DbSet<TeacherClass> TeacherClasses => Set<TeacherClass>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =============================================
            // CASCADE DELETE DISABLE — SQL Server cycle fix
            // =============================================
            modelBuilder.Entity<Attendance>()
                .HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Attendance>()
                .HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExamResult>()
                .HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ExamResult>()
                .HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ExamResult>()
                .HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Exam>()
                .HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FeePayment>()
                .HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<FeePayment>()
                .HasOne(x => x.FeeStructure).WithMany().HasForeignKey(x => x.FeeStructureId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FeeStructure>()
                .HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BookIssue>()
                .HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<BookIssue>()
                .HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StudentTransport>()
                .HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<StudentTransport>()
                .HasOne(x => x.Transport).WithMany().HasForeignKey(x => x.TransportId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Timetable>()
                .HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Timetable>()
                .HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Timetable>()
                .HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Student>()
                .HasOne(x => x.Class).WithMany(x => x.Students).HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Student>()
                .HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Teacher>()
                .HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Subject>()
                .HasOne(x => x.Class).WithMany(x => x.Subjects).HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<User>()
                .HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<TeacherClass>()
    .HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId)
    .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<TeacherClass>()
                .HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<TeacherClass>()
                .HasQueryFilter(x => x.IsActive);
            // =============================================
            // QUERY FILTERS
            // =============================================
            modelBuilder.Entity<Student>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<Teacher>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<User>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<Class>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<Subject>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<Attendance>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<BookIssue>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<Exam>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<ExamResult>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<FeePayment>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<FeeStructure>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<StudentTransport>().HasQueryFilter(x => x.IsActive);
            modelBuilder.Entity<Timetable>().HasQueryFilter(x => x.IsActive);

            // =============================================
            // DECIMAL PRECISION
            // =============================================
            modelBuilder.Entity<BookIssue>().Property(x => x.Fine).HasPrecision(10, 2);
            modelBuilder.Entity<Exam>().Property(x => x.TotalMarks).HasPrecision(10, 2);
            modelBuilder.Entity<ExamResult>().Property(x => x.ObtainedMarks).HasPrecision(10, 2);
            modelBuilder.Entity<FeePayment>().Property(x => x.AmountPaid).HasPrecision(10, 2);
            modelBuilder.Entity<FeeStructure>().Property(x => x.Amount).HasPrecision(10, 2);
            modelBuilder.Entity<Teacher>().Property(x => x.Salary).HasPrecision(10, 2);

            // =============================================
            // SEED DATA — Static values
            // =============================================
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Role { Id = 2, RoleName = "Teacher", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Role { Id = 3, RoleName = "Student", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
                new Role { Id = 4, RoleName = "Parent", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) }
            );
        }
    }
}