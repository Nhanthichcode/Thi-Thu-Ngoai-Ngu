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

        public async Task<IActionResult> Index()
        {
            return View(await _context.ReadingPassages.ToListAsync());
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
            if (item != null) { TempData["SuccessMessage"] = "Đã xóa bài đọc thành công."; }else { TempData["ErrorMessage"] = "Bài đọc không tồn tại"; }
            _context.ReadingPassages.Remove(item);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}