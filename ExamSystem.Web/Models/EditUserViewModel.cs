using System.ComponentModel.DataAnnotations;

namespace ExamSystem.Web.Models
{
    public class EditUserViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string? NewPassword { get; set; }

        public bool IsLocked { get; set; }

        // Thêm dấu ? để hệ thống không bắt buộc (ngăn lỗi ngầm)
        public string? CurrentRole { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn một vai trò")]
        public string? SelectedRole { get; set; }

        public List<string>? AllRoles { get; set; } = new List<string>();
    }
}