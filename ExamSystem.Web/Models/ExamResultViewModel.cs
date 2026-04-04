using System.ComponentModel.DataAnnotations;
namespace ExamSystem.Web.Models
{
    public class ExamResultViewModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string AvatarUrl { get; set; } // Thêm trường này
        public string ExamTitle { get; set; }
        public DateTime SubmitTime { get; set; }
        public double Score { get; set; }
        public int Status { get; set; }
    }
}