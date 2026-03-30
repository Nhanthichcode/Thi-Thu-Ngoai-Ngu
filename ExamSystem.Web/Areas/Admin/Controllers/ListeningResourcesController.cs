using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] // 2. Thêm Attribute này
    [Authorize(Roles = "Admin, Teacher")]
    public class ListeningResourcesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ListeningResourcesController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(string searchString)
        {           
            ViewData["CurrentFilter"] = searchString;
            
            var listenning = from p in _context.ListeningResources
                           select p;

            // Nếu người dùng có nhập chữ vào ô tìm kiếm thì mới lọc
            if (!String.IsNullOrEmpty(searchString))
            {
                // Lọc những bài có Tiêu đề chứa từ khóa tìm kiếm (Không phân biệt hoa thường)
                listenning = listenning.Where(s => s.Title.Contains(searchString));
            }

            // Thực thi truy vấn và trả về View
            return View(await listenning.ToListAsync());
        }

        public IActionResult Create() => View();

        // --- SỬA HÀM CREATE ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListeningResource listeningResource, IFormFile? audioFile)
        {
            // BƯỚC 1: Bỏ qua lỗi validate AudioUrl (vì ta sẽ tự gán giá trị sau khi upload)
            ModelState.Remove("AudioUrl");

            // BƯỚC 2: Xử lý Upload file
            if (audioFile != null && audioFile.Length > 0)
            {
                listeningResource.AudioUrl = await UploadFile(audioFile);
                TempData["SuccessMessage"] = "Đã thêm file âm thanh thành công.";
            }
            else { TempData["ErrorMessage"] = "Vui Lòng chọn file âm thanh hợp lệ"; }

            // BƯỚC 3: Lưu vào DB
            if (ModelState.IsValid)
            {
                _context.Add(listeningResource);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(listeningResource);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ListeningResources.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // --- SỬA HÀM EDIT ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ListeningResource listeningResource, IFormFile? audioFile)
        {
            if (id != listeningResource.Id) return NotFound();

            // Bỏ qua validate AudioUrl để xử lý logic tay
            ModelState.Remove("AudioUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy dữ liệu cũ để giữ lại AudioUrl nếu người dùng không upload file mới
                    var oldItem = await _context.ListeningResources.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                    if (audioFile != null && audioFile.Length > 0)
                    {
                        // Nếu có file mới -> Upload và cập nhật link mới
                        listeningResource.AudioUrl = await UploadFile(audioFile);
                    }
                    else
                    {
                        // Nếu không có file mới -> Giữ nguyên link cũ
                        listeningResource.AudioUrl = oldItem?.AudioUrl;
                    }

                    _context.Update(listeningResource);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ListeningResources.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(listeningResource);
        }

        // --- HÀM PHỤ ĐỂ UPLOAD FILE (TÁI SỬ DỤNG) ---
        private async Task<string> UploadFile(IFormFile file)
        {
            var fileName = DateTime.Now.Ticks.ToString() + Path.GetExtension(file.FileName);
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "audio");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/uploads/audio/" + fileName;
        }

        // DELETE (Giữ nguyên như cũ)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ListeningResources.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        // POST: ListeningResources/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Tìm bản ghi trong Database
            var listeningResource = await _context.ListeningResources.FindAsync(id);

            if (listeningResource != null)
            {
                // 2. XÓA FILE VẬT LÝ TRÊN SERVER (Quan trọng)
                if (!string.IsNullOrEmpty(listeningResource.AudioUrl))
                {
                    // Chuyển đường dẫn web (VD: /uploads/audio/abc.mp3) thành đường dẫn ổ cứng (D:\Project\wwwroot\uploads\audio\abc.mp3)
                    // TrimStart('/') để bỏ dấu / ở đầu chuỗi
                    var filePath = Path.Combine(_environment.WebRootPath, listeningResource.AudioUrl.TrimStart('/'));

                    // Kiểm tra xem file có tồn tại không rồi mới xóa
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    TempData["SuccessMessage"] = "Đã xóa file âm thanh thành công.";
                }
                else { TempData["ErrorMessage"] = "File âm thanh không tồn tại"; }
                // 3. Xóa bản ghi trong Database
                _context.ListeningResources.Remove(listeningResource);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 10. XÓA NHIỀU BÀI NGHE (POST)
        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Không có bài nghe nào được chọn." });
            }

            // Bật Transaction để đảm bảo an toàn (nếu lỗi giữa chừng thì sẽ hoàn tác)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Tìm tất cả các bài nghe có ID nằm trong danh sách được chọn
                    var resourcesToDelete = await _context.ListeningResources
                        .Where(r => ids.Contains(r.Id))
                        .ToListAsync();

                    if (!resourcesToDelete.Any())
                    {
                        return Json(new { success = false, message = "Không tìm thấy dữ liệu để xóa." });
                    }

                    int count = resourcesToDelete.Count;

                    // Xóa hàng loạt một lần duy nhất (Nhanh và tối ưu hơn dùng foreach)
                    _context.ListeningResources.RemoveRange(resourcesToDelete);
                    await _context.SaveChangesAsync();

                    // Xác nhận hoàn tất
                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"Đã xóa thành công {count} bài nghe và các câu hỏi đi kèm!"
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi hệ thống khi xóa: " + ex.Message });
                }
            }
        }
    }
}