using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
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
            // 1. Khởi tạo Query ban đầu
            var query = _context.TestAttempts
                .Include(ta => ta.Exam)
                .Include(ta => ta.User)
                .AsQueryable();

            // 2. Lọc theo Tên hoặc Email
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(ta => ta.User.FullName.Contains(searchTerm) || ta.User.Email.Contains(searchTerm));
            }

            // 3. Lọc theo Đề thi cụ thể
            if (examId.HasValue)
            {
                query = query.Where(ta => ta.ExamId == examId);
            }

            // 4. Lọc theo Trạng thái (Đã chấm / Chờ chấm)
            if (status.HasValue)
            {
                query = query.Where(ta => ta.Status == status.Value);
            }

            // 5. Lọc theo Mốc thời gian (3, 5, 7, 21 ngày...)
            if (dateRange.HasValue)
            {
                var cutoffDate = DateTime.Now.AddDays(-dateRange.Value);
                query = query.Where(ta => ta.SubmitTime >= cutoffDate);
            }

            // 6. Thực thi truy vấn và sắp xếp mới nhất lên đầu
            var attempts = await query.OrderByDescending(ta => ta.SubmitTime).ToListAsync();

            // Gửi danh sách đề thi qua ViewBag để làm Dropdown
            ViewBag.Exams = await _context.Exams.OrderBy(e => e.Title).ToListAsync();

            // Giữ lại các giá trị lọc trên giao diện
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

            // Trả về mã thành công 200 OK cho AJAX
            return Ok();
        }

        // Trang chi tiết để giáo viên xem file và chấm điểm
        public async Task<IActionResult> Grade(int id)
        {
            var attempt = await _context.TestAttempts
                .Include(ta => ta.Exam)
                .Include(ta => ta.User)
                .Include(ta => ta.TestResults)
                    .ThenInclude(tr => tr.Question)
                .FirstOrDefaultAsync(ta => ta.Id == id);

            if (attempt == null) return NotFound();

            // LỌC DỮ LIỆU ĐỂ HIỂN THỊ:
            // Chỉ giữ lại câu Tự luận (Essay) và Thu âm (SpeakingRecording)
            // Để giáo viên tập trung chấm các câu này.
            attempt.TestResults = attempt.TestResults
                .Where(tr => tr.Question.QuestionType == QuestionType.Essay ||
                             tr.Question.QuestionType == QuestionType.SpeakingRecording)
                .OrderBy(tr => tr.Id) // Sắp xếp theo thứ tự xuất hiện trong bài làm
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
