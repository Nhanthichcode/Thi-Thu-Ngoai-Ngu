using System.ComponentModel.DataAnnotations;

public class ForgotPasswordVM
{
    [Required(ErrorMessage = "Vui lòng nhập thông tin.")]
    public string Identifier { get; set; } // Có thể là Email hoặc Phone
    //public string Method { get; set; } // Email, SMS, hoặc Admin
}