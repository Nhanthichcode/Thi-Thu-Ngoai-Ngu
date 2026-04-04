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


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Tìm bản ghi kèm theo danh sách câu hỏi con
            var item = await _context.ReadingPassages
                .Include(r => r.Questions) // Quan trọng: Tải các câu hỏi liên quan
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item != null)
            {
                // 2. Xóa các câu hỏi con trước để tránh bị "mồ côi" trong ngân hàng
                if (item.Questions != null && item.Questions.Any())
                {
                    _context.Questions.RemoveRange(item.Questions);
                }

                // 3. Xóa bài đọc
                _context.ReadingPassages.Remove(item);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa bài đọc {item.Title} và các câu hỏi liên quan thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Bài đọc không tồn tại";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Không có bài đọc nào được chọn." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tìm tất cả bài đọc kèm theo câu hỏi của chúng
                    var passagesToDelete = await _context.ReadingPassages
                        .Include(r => r.Questions)
                        .Where(r => ids.Contains(r.Id))
                        .ToListAsync();

                    if (!passagesToDelete.Any())
                    {
                        return Json(new { success = false, message = "Không tìm thấy dữ liệu để xóa." });
                    }

                    // 2. Thu thập tất cả câu hỏi thuộc về những bài đọc này
                    var questionsToDelete = passagesToDelete.SelectMany(p => p.Questions).ToList();

                    // 3. Thực hiện xóa câu hỏi trước, xóa bài đọc sau
                    if (questionsToDelete.Any())
                    {
                        _context.Questions.RemoveRange(questionsToDelete);
                    }

                    _context.ReadingPassages.RemoveRange(passagesToDelete);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"Đã xóa thành công {passagesToDelete.Count} bài đọc và {questionsToDelete.Count} câu hỏi liên quan!"
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