using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Teacher")]
    public class ExamResultsController : Controller
    {
        private readonly AppDbContext _context;

        public ExamResultsController(AppDbContext context)
        {
            _context = context;
        }

        // Xem danh sách tất cả các lượt thi của thí sinh
        public async Task<IActionResult> Index(string searchTerm, int? examId, int? dateRange, int? status)
        {
            var query = _context.TestAttempts.AsQueryable();

            // 1. Logic lọc (Giữ nguyên)
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(ta => ta.User.FullName.Contains(searchTerm) || ta.User.Email.Contains(searchTerm));

            if (examId.HasValue) query = query.Where(ta => ta.ExamId == examId);
            if (status.HasValue) query = query.Where(ta => ta.Status == status.Value);
            if (dateRange.HasValue)
            {
                var cutoffDate = DateTime.Now.AddDays(-dateRange.Value);
                query = query.Where(ta => ta.SubmitTime >= cutoffDate);
            }

            // 2. Map dữ liệu sang ViewModel để View hiểu được và lấy được Score
            var attempts = await query
                .OrderByDescending(ta => ta.SubmitTime)
                .Select(ta => new ExamResultViewModel
                {
                    Id = ta.Id,
                    StudentName = ta.User.FullName ?? "Học viên ẩn danh",
                    StudentEmail = ta.User.Email ?? "Không có email",
                    AvatarUrl = ta.User.AvatarUrl,
                    ExamTitle = ta.Exam.Title,
                    SubmitTime = ta.SubmitTime ?? DateTime.Now,
                    Score = ta.Score, // Lấy điểm từ DB
                    Status = ta.Status
                })
                .ToListAsync();

            ViewBag.Exams = await _context.Exams.OrderBy(e => e.Title).ToListAsync();

            // Giữ filter
            ViewData["SearchTerm"] = searchTerm;
            ViewData["SelectedExam"] = examId;
            ViewData["SelectedDate"] = dateRange;
            ViewData["SelectedStatus"] = status;

            return View(attempts);
        }

        // POST: Admin/ExamResults/Delete/20
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var examResult = await _context.TestAttempts.FindAsync(id);

            if (examResult == null)
            {
                // Trả về mã lỗi 404 cho AJAX
                return NotFound();
            }

            _context.TestAttempts.Remove(examResult);
            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] = "Đã xóa bài thi thành công";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Không có bài nộp nào được chọn." });
            }

            // Bật Transaction để đảm bảo an toàn (nếu lỗi giữa chừng thì sẽ hoàn tác)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Tìm tất cả các KẾT QUẢ BÀI THI có ID nằm trong danh sách được chọn
                    var attemptsToDelete = await _context.TestAttempts
                        .Where(t => ids.Contains(t.Id))
                        .ToListAsync();

                    if (!attemptsToDelete.Any())
                    {
                        return Json(new { success = false, message = "Không tìm thấy dữ liệu để xóa." });
                    }

                    int count = attemptsToDelete.Count;

                    // Xóa hàng loạt một lần duy nhất (Nhanh và tối ưu hơn dùng foreach)
                    _context.TestAttempts.RemoveRange(attemptsToDelete);
                    await _context.SaveChangesAsync();

                    // Xác nhận hoàn tất
                    await transaction.CommitAsync();

                    // Setup TempData để khi JS gọi reload trang sẽ hiện thông báo thành công
                    TempData["SuccessMessage"] = $"Đã xóa thành công {count} bài nộp của thí sinh!";

                    return Json(new
                    {
                        success = true,
                        message = $"Đã xóa thành công {count} bài nộp!"
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi hệ thống khi xóa: " + ex.Message });
                }
            }
        }

        // Trang chi tiết để giáo viên xem file và chấm điểm
        public async Task<IActionResult> Grade(int id)
        {
            // Đảm bảo Include đầy đủ các cấp độ dữ liệu
            var attempt = await _context.TestAttempts
                .Include(ta => ta.Exam)
                .Include(ta => ta.User)
                .Include(ta => ta.TestResults)
                    .ThenInclude(tr => tr.Question)
                .FirstOrDefaultAsync(ta => ta.Id == id);

            if (attempt == null) return NotFound();

            // Kiểm tra xem TestResults có dữ liệu chưa trước khi lọc
            if (attempt.TestResults == null || !attempt.TestResults.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu câu trả lời cho lượt thi này.";
                return RedirectToAction(nameof(Index));
            }

            // Tạm thời bỏ lọc để kiểm tra xem có dữ liệu hay không, 
            // Nếu có dữ liệu rồi thì mới bật lại lọc QuestionType
            attempt.TestResults = attempt.TestResults
                .Where(tr => tr.Question != null &&
                            (tr.Question.QuestionType == QuestionType.Essay ||
                             tr.Question.QuestionType == QuestionType.SpeakingRecording))
                .OrderBy(tr => tr.Id)
                .ToList();

            return View(attempt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrades(int attemptId, Dictionary<int, double> scores, Dictionary<int, string> notes)
        {
            // 1. Tải TOÀN BỘ kết quả (bao gồm cả Trắc nghiệm đã chấm tự động)
            // Phải include Question để check QuestionType
            var allResults = await _context.TestResults
                .Include(tr => tr.Question)
                .Where(tr => tr.TestAttemptId == attemptId)
                .ToListAsync();

            if (!allResults.Any()) return NotFound();

            // 2. Duyệt qua từng câu hỏi và cập nhật điểm
            foreach (var result in allResults)
            {
                // [QUAN TRỌNG]: Chỉ cho phép sửa điểm nếu là câu Tự luận hoặc Nói
                // Bỏ qua câu Trắc nghiệm (SingleChoice/MultipleChoice) để bảo toàn điểm máy chấm
                if (result.Question.QuestionType == QuestionType.Essay ||
                    result.Question.QuestionType == QuestionType.SpeakingRecording)
                {
                    // Kiểm tra xem giáo viên có gửi điểm cho câu này không
                    if (scores.ContainsKey(result.Id))
                    {
                        result.ScoreObtained = scores[result.Id];

                        // Lưu nhận xét (Feedback) nếu có
                        if (notes.ContainsKey(result.Id))
                        {
                            result.Feedback = notes[result.Id];
                        }
                    }
                }
                else
                {
                    // Với câu trắc nghiệm, vẫn cho phép lưu nhận xét (nếu giáo viên muốn góp ý thêm)
                    // Nhưng KHÔNG cập nhật điểm số.
                    if (notes.ContainsKey(result.Id))
                    {
                        result.Feedback = notes[result.Id];
                    }
                }
            }

            // 3. Tính toán lại Tổng điểm và Trạng thái
            var attempt = await _context.TestAttempts.FindAsync(attemptId);
            if (attempt != null)
            {
                // Tổng điểm = Điểm trắc nghiệm (giữ nguyên) + Điểm tự luận (vừa chấm)
                // Dùng ?? 0 để xử lý trường hợp null
                attempt.Score = allResults.Sum(r => r.ScoreObtained);

                // Cập nhật trạng thái
                attempt.Status = (int)TestStatus.Graded;

                // Kiểm tra lại tên thuộc tính trong Entity của bạn (IsGraded hay isGraded)
                // attempt.IsGraded = true; 
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã chấm xong! Tổng điểm mới: {attempt?.Score}";
            return RedirectToAction(nameof(Index));
        }
    }
}
