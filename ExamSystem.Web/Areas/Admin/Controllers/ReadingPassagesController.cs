using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] // 2. Thêm Attribute này
    [Authorize(Roles = "Admin, Teacher")]
    public class ReadingPassagesController : Controller
    {
        private readonly AppDbContext _context;
        public ReadingPassagesController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var passages = from p in _context.ReadingPassages
                           select p;
            if (!String.IsNullOrEmpty(searchString))
            {
                passages = passages.Where(s => s.Title.Contains(searchString));
            }           
            return View(await passages.ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReadingPassage readingPassage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(readingPassage);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm bài đọc thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(readingPassage);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ReadingPassages.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReadingPassage readingPassage)
        {
            if (id != readingPassage.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(readingPassage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(readingPassage);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ReadingPassages.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.ReadingPassages.FindAsync(id);
            if (item != null) { TempData["SuccessMessage"] = "Đã xóa bài đọc thành công."; } else { TempData["ErrorMessage"] = "Bài đọc không tồn tại"; }
            _context.ReadingPassages.Remove(item);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    
     // 10. XÓA NHIỀU BÀI NGHE (POST)
        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Không có bài đọc nào được chọn." });
            }

            // Bật Transaction để đảm bảo an toàn (nếu lỗi giữa chừng thì sẽ hoàn tác)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Tìm tất cả các bài nghe có ID nằm trong danh sách được chọn
                    var resourcesToDelete = await _context.ReadingPassages
                        .Where(r => ids.Contains(r.Id))
                        .ToListAsync();

                    if (!resourcesToDelete.Any())
                    {
                        return Json(new { success = false, message = "Không tìm thấy dữ liệu để xóa." });
                    }

                    int count = resourcesToDelete.Count;

                    // Xóa hàng loạt một lần duy nhất (Nhanh và tối ưu hơn dùng foreach)
                    _context.ReadingPassages.RemoveRange(resourcesToDelete);
                    await _context.SaveChangesAsync();

                    // Xác nhận hoàn tất
                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"Đã xóa thành công {count} bài đọc và các câu hỏi đi kèm!"
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