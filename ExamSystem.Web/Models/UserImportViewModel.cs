namespace ExamSystem.Web.Models
{
    public class UserImportViewModel
    {
        public string Email { get; set; }     // Tên đăng nhập
        public string FullName { get; set; }  // Tên người dùng
        public string Password { get; set; }  // Mật khẩu
    }
}