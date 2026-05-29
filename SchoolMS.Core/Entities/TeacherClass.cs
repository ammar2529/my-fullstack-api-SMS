namespace SchoolMS.Core.Entities
{
    public class TeacherClass : BaseEntity
    {
        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }
        public int ClassId { get; set; }
        public Class? Class { get; set; }
    }
}