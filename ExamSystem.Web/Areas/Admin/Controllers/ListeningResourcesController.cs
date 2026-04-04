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

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // 1. Tìm bản ghi và kèm theo danh sách Questions của nó
            var listeningResource = await _context.ListeningResources
                .Include(r => r.Questions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (listeningResource != null)
            {
                // 2. XÓA FILE VẬT LÝ (Giữ nguyên logic của bạn)
                if (!string.IsNullOrEmpty(listeningResource.AudioUrl))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, listeningResource.AudioUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // 3. XÓA CÁC CÂU HỎI CON TRƯỚC
                if (listeningResource.Questions != null && listeningResource.Questions.Any())
                {
                    _context.Questions.RemoveRange(listeningResource.Questions);
                }

                // 4. Xóa bản ghi chính trong Database
                _context.ListeningResources.Remove(listeningResource);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa bài {listeningResource.Title} và các câu hỏi liên quan.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Không có bài nghe nào được chọn." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tìm các bài nghe kèm theo câu hỏi
                    var resourcesToDelete = await _context.ListeningResources
                        .Include(r => r.Questions)
                        .Where(r => ids.Contains(r.Id))
                        .ToListAsync();

                    if (!resourcesToDelete.Any())
                    {
                        return Json(new { success = false, message = "Không tìm thấy dữ liệu để xóa." });
                    }

                    // 2. Thu thập tất cả câu hỏi thuộc các bài nghe này
                    var questionsToDelete = resourcesToDelete
                        .SelectMany(r => r.Questions)
                        .ToList();

                    // 3. Xóa file vật lý của từng bài (Nên làm để dọn dẹp server)
                    foreach (var res in resourcesToDelete)
                    {
                        if (!string.IsNullOrEmpty(res.AudioUrl))
                        {
                            var filePath = Path.Combine(_environment.WebRootPath, res.AudioUrl.TrimStart('/'));
                            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                        }
                    }

                    // 4. Xóa câu hỏi trước, xóa bài nghe sau
                    if (questionsToDelete.Any())
                    {
                        _context.Questions.RemoveRange(questionsToDelete);
                    }
                    _context.ListeningResources.RemoveRange(resourcesToDelete);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"Đã xóa thành công {resourcesToDelete.Count} bài nghe và {questionsToDelete.Count} câu hỏi đi kèm!"
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }

    }
}