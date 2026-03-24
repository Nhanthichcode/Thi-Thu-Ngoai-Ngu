using Azure.Core;
using ExamSystem.Core.Entities;
using ExamSystem.Web.Models;
//using ExamSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System;
using System.Diagnostics;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IEmailSender _emailSender;

    public AccountController(UserManager<AppUser> userManager, 
        SignInManager<AppUser> signInManager, 
        IWebHostEnvironment webHostEnvironment, 
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _webHostEnvironment = webHostEnvironment;
        _emailSender = emailSender;
    }

    // --- ĐĂNG KÝ ---
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        if (ModelState.IsValid)
        {
            var user = new AppUser { UserName = model.Email, Email = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student"); // Mặc định là Student
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View(model);
    }

    // --- ĐĂNG NHẬP ---
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                //TempData["SuccessMessage"] = "Liên kết đặt lại mật khẩu đang được gửi vào Email của bạn."; 
                TempData["ErrorMessage"] = "Tài khoản không tồn tại";
                return View(model);
            }

            // 2. Kiểm tra xem Admin có đang khóa tài khoản này không (Dùng đúng logic của bạn)
            if (await _userManager.IsLockedOutAsync(user))
            {
                return RedirectToAction("Lockout", "Account");
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                //var user = await _userManager.FindByEmailAsync(model.Email);

                if (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Teacher"))
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                if (await _userManager.IsInRoleAsync(user, "Student"))
                {
                    return RedirectToAction("Index", "Home", new { area = "" });
                }
                TempData["SuccessMessage"] = "Đăng nhập thành công";
                // Mặc định cho khách -> Về trang chủ
                return RedirectToAction("Index", "Home");
            }
            TempData["ErrorMessage"] = "Tên đăng nhập hoặc mật khẩu không đúng";
        }
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous] // Quan trọng: Cho phép truy cập ngay cả khi chưa đăng nhập
    public IActionResult Lockout()
    {
        return View();
    }
    // --- GOOGLE LOGIN ---

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi từ dịch vụ ngoài: {remoteError}");
            return View("Login");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ModelState.AddModelError(string.Empty, "Lỗi khi tải thông tin đăng nhập Google.");
            return View("Login");
        }

        var googleAvatarUrl = info.Principal.FindFirstValue("urn:google:picture");

        // Nếu vẫn null, thử tìm fallback (phòng trường hợp Google đổi cấu trúc)
        if (string.IsNullOrEmpty(googleAvatarUrl))
        {
            googleAvatarUrl = info.Principal.FindFirstValue("picture");
        }

        // 1. Nếu đã có tài khoản liên kết -> Đăng nhập
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            // (Tùy chọn) Cập nhật lại ảnh Google mới nhất nếu user chưa có ảnh riêng
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            // Điều kiện: User tồn tại + Có ảnh từ Google + (User chưa có ảnh HOẶC đang dùng ảnh Google cũ)
            if (user != null && !string.IsNullOrEmpty(googleAvatarUrl))
            {
                // Chỉ cập nhật nếu user chưa tự upload ảnh riêng (ảnh upload riêng sẽ bắt đầu bằng /uploads/)
                if (string.IsNullOrEmpty(user.AvatarUrl) || !user.AvatarUrl.StartsWith("/uploads/"))
                {
                    // Chỉ update nếu link mới khác link cũ (tránh update thừa)
                    if (user.AvatarUrl != googleAvatarUrl)
                    {
                        user.AvatarUrl = googleAvatarUrl;
                        await _userManager.UpdateAsync(user);
                        // Refresh lại session để header cập nhật ảnh ngay
                        await _signInManager.RefreshSignInAsync(user);
                    }
                }
            }
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut) return RedirectToAction("Lockout");

        // 2. Nếu chưa có tài khoản -> Đăng ký tự động
        else
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (email != null)
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    // TRƯỜNG HỢP A: Tạo User mới (Lưu luôn AvatarUrl vào đây)
                    user = new AppUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = name,
                        AvatarUrl = googleAvatarUrl // [QUAN TRỌNG] Gán ảnh ngay lúc tạo
                    };

                    var resultCreate = await _userManager.CreateAsync(user);

                    if (resultCreate.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Student");
                        await _userManager.AddLoginAsync(user, info);
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                else
                {
                    // TRƯỜNG HỢP B: Đã có User cũ -> Liên kết Google
                    // Nếu user cũ chưa có ảnh thì lấy ảnh Google đắp vào
                    if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(googleAvatarUrl))
                    {
                        user.AvatarUrl = googleAvatarUrl;
                        await _userManager.UpdateAsync(user);
                    }

                    var resultAddLogin = await _userManager.AddLoginAsync(user, info);
                    if (resultAddLogin.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }
    }

    // --- ĐĂNG XUẤT ---
    [HttpPost]
    [ValidateAntiForgeryToken] // Bảo mật: Chỉ nhận lệnh từ nút bấm trong trang web
    public async Task<IActionResult> Logout()
    {
        // 1. Xóa Cookie đăng nhập
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    // --- PROFILE, ĐỔI MẬT KHẨU & XÓA ẢNH CŨ ---

    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var model = new UserProfileVM
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            NewPassword = "", // Khởi tạo rỗng để hiển thị ô nhập
            DateOfBirth = user.DateOfBirth,
            AvatarUrl = user.AvatarUrl
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Student")] // Tùy vào thiết kế, bạn có thể chỉ dùng [Authorize] để áp dụng cho mọi Role
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // 1. Xử lý Cập nhật Mật khẩu (Nếu người dùng có nhập pass mới)
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            bool isSamePassword = await _userManager.CheckPasswordAsync(user, model.NewPassword);
            if (isSamePassword)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng với mật khẩu hiện tại!");
                return View(model);
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (!passResult.Succeeded)
            {
                foreach (var error in passResult.Errors)
                {
                    ModelState.AddModelError("NewPassword", error.Description);
                }
                return View(model);
            }
        }

        // 2. Xử lý Upload Ảnh Mới
        if (model.AvatarUpload != null)
        {
            // A. XÓA ẢNH CŨ TRƯỚC KHI LƯU ẢNH MỚI
            // Chỉ xóa nếu ảnh cũ nằm trong thư mục uploads (không xóa ảnh link Google nếu có đăng nhập MXH)
            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/"))
            {
                // Chuyển đường dẫn web (/uploads/...) thành đường dẫn ổ cứng
                var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath); // Xóa file vật lý
                }
            }

            // B. LƯU ẢNH MỚI
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "user_avatars");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.AvatarUpload.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.AvatarUpload.CopyToAsync(fileStream);
            }

            // Cập nhật đường dẫn mới vào User
            user.AvatarUrl = "/uploads/user_avatars/" + uniqueFileName;
        }

        // 3. Cập nhật các thông tin cơ bản khác
        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.DateOfBirth = model.DateOfBirth;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            // Refresh lại phiên đăng nhập để thông tin (như Tên, Avatar) cập nhật ngay trên Header
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction(nameof(Profile));
        }

        // Nếu UpdateAsync có lỗi (VD: trùng số điện thoại...)
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }

    // --- XÓA ẢNH ĐẠI DIỆN ---
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAvatar()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // 1. Nếu đang có ảnh
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            // Kiểm tra xem có phải ảnh lưu trên server mình không (bắt đầu bằng /uploads/)
            if (user.AvatarUrl.StartsWith("/uploads/"))
            {
                // Xóa file vật lý để giải phóng bộ nhớ
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // 2. Xóa đường dẫn trong Database (về null)
            user.AvatarUrl = null;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Refresh lại cookie để Header cập nhật ngay lập tức
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Đã xóa ảnh đại diện.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa ảnh.";
            }
        }
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword()
    {
        TempData["SuccessMessage"] = "Tính năng đang được phát triển.";
        return View();        
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
    {
        // 1. Bây giờ dòng này sẽ trả về True vì không còn thiếu trường Method nữa
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Identifier)
                   ?? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.Identifier);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Thông tin không khớp với bất kỳ tài khoản nào.";
            return View(model);
        }
     
        //try
        //{
        //    TempData["SuccessMessage"] = "Liên kết đặt lại mật khẩu đang được gửi vào Email của bạn.";
        //    // 2. Chạy thẳng vào logic gửi Email luôn, không cần switch case
        //    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        //    var callbackUrl = Url.Action("ResetPassword", "Account",
        //        new { token = token, email = user.Email }, Request.Scheme);

        //    string subject = "Đặt lại mật khẩu - Thi Thử Ngoại Ngữ";
        //    string message = $@" ... (Nội dung HTML của bạn) ... ";

        //    await _emailSender.SendEmailAsync(user.Email, subject, message);

        //    TempData["SuccessMessage"] = "Liên kết đặt lại mật khẩu đã được gửi vào Email của bạn.";
        //}
        //catch (Exception ex)
        //{
        //    TempData["ErrorMessage"] = "Lỗi gửi mail: " + ex.Message;
        //    return View(model);
        //}

        return RedirectToAction("ForgotPassword");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string token = null, string email = null)
    {
        if (token == null || email == null)
        {
            return RedirectToAction("Error", "Home");
        }

        var model = new ResetPasswordVM
        {
            Token = token,
            Email = email
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return RedirectToAction("ResetPasswordConfirmation");

        // Thực hiện Reset
        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (result.Succeeded)
        {
            return RedirectToAction("ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View();
    }
}