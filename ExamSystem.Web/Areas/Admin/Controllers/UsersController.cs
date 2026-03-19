using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;
using ExamSystem.Web.Models;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được vào
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 1. Danh sách người dùng
        //[HttpGet]
        //public async Task<IActionResult> Index()
        //{
        //    var users = await _userManager.Users.ToListAsync();

        //    // Lấy thêm Role cho từng user để hiển thị (Option)
        //    // Lưu ý: Logic này có thể chậm nếu user quá đông, cần phân trang
        //    var userViewModels = new List<UserViewModel>(); // Bạn cần tạo class UserViewModel
        //    foreach (var user in users)
        //    {
        //        var roles = await _userManager.GetRolesAsync(user);
        //        userViewModels.Add(new UserViewModel
        //        {
        //            Id = user.Id,
        //            Email = user.Email,
        //            FullName = user.FullName,
        //            AvatarUrl = user.AvatarUrl,
        //            PhoneNumber = user.PhoneNumber,
        //            Roles = string.Join(", ", roles),
        //            IsLocked = await _userManager.IsLockedOutAsync(user)
        //        });
        //    }

        //    return View(userViewModels);
        //}
        [HttpGet]
        public async Task<IActionResult> Index(string searchQuery, string role, string status)
        {
            // Truyền dữ liệu bộ lọc xuống View để giữ trạng thái
            ViewData["CurrentSearch"] = searchQuery;
            ViewData["CurrentRole"] = role;
            ViewData["CurrentStatus"] = status;

            var usersQuery = _userManager.Users.AsQueryable();

            // Lọc theo từ khóa (Tên hoặc Email)
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim().ToLower();
                usersQuery = usersQuery.Where(u => u.FullName.ToLower().Contains(searchQuery) || u.Email.ToLower().Contains(searchQuery));
            }

            var users = await usersQuery.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                bool isLocked = await _userManager.IsLockedOutAsync(user);

                // Lọc theo Vai trò
                if (!string.IsNullOrEmpty(role) && !roles.Contains(role)) continue;

                // Lọc theo Trạng thái
                if (status == "active" && isLocked) continue;
                if (status == "locked" && !isLocked) continue;

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl,
                    PhoneNumber = user.PhoneNumber,
                    Roles = string.Join(", ", roles),
                    IsLocked = isLocked
                });
            }

            return View(userViewModels);
        }

        // 2. Khóa / Mở khóa tài khoản
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null); // Mở khóa
                TempData["SuccessMessage"] = $"Đã mở khóa {user.Email}";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100)); // Khóa vĩnh viễn
                TempData["SuccessMessage"] = $"Đã khóa {user.Email}";
            }

            return RedirectToAction(nameof(Index));
        }

        // 3. CHỈNH SỬA NGƯỜI DÙNG (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CurrentRole = userRoles.FirstOrDefault(),
                SelectedRole = userRoles.FirstOrDefault(),
                AllRoles = allRoles,
                IsLocked = await _userManager.IsLockedOutAsync(user)
            };

            return View(model);
        }

        // 4. CHỈNH SỬA NGƯỜI DÙNG (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // A. Cập nhật thông tin cơ bản
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber; // Cập nhật lại sđt

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                model.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            // B. Cập nhật Vai trò
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(model.SelectedRole))
            {
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
            }

            // C. Xử lý Ghi đè Mật khẩu
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

                if (!passResult.Succeeded)
                {
                    foreach (var error in passResult.Errors) ModelState.AddModelError("", error.Description);
                    model.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                    return View(model);
                }
            }

            // D. FIX LỖI TREO KHI KHÓA: Sử dụng DateTimeOffset.UtcNow.AddYears(100) thay vì MaxValue
            if (model.IsLocked)
            {
                // Khóa 100 năm là đủ dài để coi như vĩnh viễn và an toàn cho SQL Server
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }

            TempData["SuccessMessage"] = "Đã cập nhật thông tin người dùng thành công!";
            return RedirectToAction(nameof(Index));
        }

        // 5. Xóa tài khoản
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["SuccessMessage"] = "Đã xóa người dùng.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. TẠO MỚI NGƯỜI DÙNG (GET)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateUserViewModel
            {
                AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync()
            };
            return View(model);
        }

        // 7. TẠO MỚI NGƯỜI DÙNG (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            // Tạo user mới (Dùng Email làm UserName luôn)
            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true // Mặc định xác nhận email để admin tạo là dùng được ngay
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Gán quyền cho user mới
                if (!string.IsNullOrEmpty(model.SelectedRole))
                {
                    await _userManager.AddToRoleAsync(user, model.SelectedRole);
                }

                TempData["SuccessMessage"] = $"Đã tạo thành công tài khoản: {user.Email}";
                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi (ví dụ trùng Email)
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // 8. IMPORT NGƯỜI DÙNG TỪ EXCEL (GET)
        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        // 9. XỬ LÝ IMPORT NGƯỜI DÙNG HÀNG LOẠT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessImport([FromBody] List<UserImportViewModel> users)
        {
            if (users == null || !users.Any())
                return Json(new { success = false, message = "Không có dữ liệu hợp lệ để import." });

            int successCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            foreach (var u in users)
            {
                // Kiểm tra null/rỗng
                if (string.IsNullOrWhiteSpace(u.Email) || string.IsNullOrWhiteSpace(u.FullName) || string.IsNullOrWhiteSpace(u.Password))
                {
                    errorCount++;
                    continue;
                }

                // Dùng Email làm UserName
                var user = new AppUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    FullName = u.FullName,
                    EmailConfirmed = true // Mặc định kích hoạt luôn
                };

                // Tạo tài khoản với mật khẩu từ Excel
                var result = await _userManager.CreateAsync(user, u.Password);

                if (result.Succeeded)
                {
                    // Gán vai trò mặc định (Ví dụ: Student). Bạn có thể đổi thành vai trò khác.
                    await _userManager.AddToRoleAsync(user, "Student");
                    successCount++;
                }
                else
                {
                    errorCount++;
                    errors.Add($"Lỗi tài khoản {u.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            return Json(new
            {
                success = true,
                successCount = successCount,
                errorCount = errorCount,
                errors = errors
            });
        }
    }
}