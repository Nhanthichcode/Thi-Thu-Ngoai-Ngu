using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Web.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn một vai trò")]
        public string? SelectedRole { get; set; }

        public List<string>? AllRoles { get; set; } = new List<string>();
    }
}