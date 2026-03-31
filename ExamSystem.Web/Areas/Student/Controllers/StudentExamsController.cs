using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExamSystem.Web.Areas.Student.Controllers
{
    [Area("Student")] // 2. Thêm Attribute này
    // [Authorize(Roles = "Student")]
    public class StudentExamsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // 1. Inject môi trường
        private readonly UserManager<AppUser> _userManager;
        public StudentExamsController(
        AppDbContext context,
        IWebHostEnvironment webHostEnvironment,
        UserManager<AppUser> userManager) // <--- Thêm tham số này
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager; // <--- Gán giá trị
        }

        // GET: /Test/Index       
        public async Task<IActionResult> Index(string searchName, string durationRange)
        {
            // 1. Khởi tạo câu truy vấn cơ bản
            var query = _context.Exams
                .Include(e => e.ExamParts)
                .Where(e => e.IsActive == true);

            // 2. Lọc theo tên đề thi (nếu có)
            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(e => e.Title.Contains(searchName));
            }

            // 3. Lọc theo khoảng thời gian (nếu có)
            if (!string.IsNullOrEmpty(durationRange))
            {
                switch (durationRange)
                {
                    case "30": query = query.Where(e => e.DurationMinutes <= 30); break;
                    case "60": query = query.Where(e => e.DurationMinutes > 30 && e.DurationMinutes <= 60); break;
                    case "90": query = query.Where(e => e.DurationMinutes > 60 && e.DurationMinutes <= 90); break;
                    case "120": query = query.Where(e => e.DurationMinutes > 90 && e.DurationMinutes <= 120); break;
                    case "120plus": query = query.Where(e => e.DurationMinutes > 120); break;
                }
            }

            // Lấy danh sách đề thi sau khi lọc
            var exams = await query.OrderByDescending(e => e.Id).ToListAsync();

            // 4. Lấy danh sách ID các đề thi mà User này đã từng làm
            var userId = _userManager.GetUserId(User);
            List<int> completedExamIds = new List<int>();

            if (!string.IsNullOrEmpty(userId))
            {
                completedExamIds = await _context.TestAttempts
                    .Where(ta => ta.UserId == userId)
                    .Select(ta => ta.ExamId)
                    .Distinct()
                    .ToListAsync();
            }

            // Gửi dữ liệu filter và danh sách đề đã làm sang View
            ViewBag.SearchName = searchName;
            ViewBag.DurationRange = durationRange;
            ViewBag.CompletedExamIds = completedExamIds;

            return View(exams);
        }

        public async Task<IActionResult> Take(int id)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamParts)
                    .ThenInclude(ep => ep.ExamQuestions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Answers)
                .Include(e => e.ExamParts)
                    .ThenInclude(ep => ep.ExamQuestions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.ReadingPassage)
                .Include(e => e.ExamParts)
                    .ThenInclude(ep => ep.ExamQuestions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.ListeningResource)
                .AsSplitQuery()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null) return NotFound();

            // Sắp xếp các Part theo OrderIndex để đảm bảo Part 1, Part 2, Part 3 hiện đúng thứ tự
            exam.ExamParts = exam.ExamParts.OrderBy(p => p.OrderIndex).ToList();

            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int examId,
            Dictionary<int, int> answers,
            Dictionary<int, string> essayAnswers)
        // Lưu ý: Tôi đã bỏ tham số audioAnswers ở đây để tự xử lý bên dưới cho chắc chắn
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");
            string studentName = currentUser.FullName ?? currentUser.UserName ?? "ThiSinh";

            // Làm sạch tên sinh viên để dùng làm tên thư mục (thay khoảng trắng bằng dấu gạch dưới, xóa ký tự đặc biệt)
            string safeStudentName = studentName.Replace(" ", "_");
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                safeStudentName = safeStudentName.Replace(c.ToString(), "");
            }

            // 1. Khởi tạo các Dictionary để tránh Null Reference
            answers = answers ?? new Dictionary<int, int>();
            essayAnswers = essayAnswers ?? new Dictionary<int, string>();

            // 2. Lấy đề thi
            var examQuestions = await _context.ExamQuestions
                .Include(eq => eq.Question).ThenInclude(q => q.Answers)
                .Where(eq => eq.ExamPart.ExamId == examId)
                .AsNoTracking().ToListAsync();

            if (!examQuestions.Any()) return NotFound("Lỗi: Không tìm thấy đề thi.");

            // 3. Tạo lượt thi
            var attempt = new TestAttempt
            {
                UserId = userId,
                ExamId = examId,
                StartTime = DateTime.Now.AddMinutes(-60),
                SubmitTime = DateTime.Now,
                Status = (int)TestStatus.Graded
            };

            var resultsList = new List<TestResult>();
            double totalScore = 0;
            bool hasManualGrading = false;

            // 4. CHUẨN BỊ THƯ MỤC UPLOAD
            // Chỉ tạo khi có file thực sự, nhưng ta cứ lấy đường dẫn trước
            string dateTimeStr = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
            string folderName = $"{safeStudentName}-{dateTimeStr}";

            // Đường dẫn gốc trên server
            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "student_exams", folderName);

            // ==========================================================================================
            // [FIX QUAN TRỌNG]: LẤY FILE THỦ CÔNG TỪ REQUEST
            // ==========================================================================================
            var uploadedFiles = Request.Form.Files; // Lấy toàn bộ file được gửi lên
            if (uploadedFiles.Count > 0)
            {
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            }

            foreach (var eq in examQuestions)
            {
                var result = new TestResult { QuestionId = eq.QuestionId };

                // --- A. TRẮC NGHIỆM ---
                if (answers.ContainsKey(eq.QuestionId))
                {
                    int studentAnsId = answers[eq.QuestionId];
                    result.SelectedAnswerId = studentAnsId;
                    var correctAnswer = eq.Question.Answers.FirstOrDefault(a => a.IsCorrect == true);
                    if (correctAnswer != null && correctAnswer.Id == studentAnsId)
                    {
                        result.IsCorrect = true; result.ScoreObtained = eq.Score; totalScore += eq.Score;
                    }
                    else
                    {
                        result.IsCorrect = false; result.ScoreObtained = 0;
                    }
                }
                // --- B. TỰ LUẬN (WRITING) ---
                else if (essayAnswers.ContainsKey(eq.QuestionId))
                {
                    result.TextAnswer = essayAnswers[eq.QuestionId];
                    result.IsCorrect = null;
                    hasManualGrading = true;
                }
                // --- C. GHI ÂM (SPEAKING) - XỬ LÝ MỚI ---
                else if (eq.Question.SkillType == ExamSystem.Core.Enums.ExamSkill.Speaking)
                {
                    // Tìm file trong Request có name khớp với "audioAnswers[QuestionId]"
                    // Name trong HTML là: audioAnswers[105] -> Ta tìm file nào có Name chứa [105]
                    var file = uploadedFiles.FirstOrDefault(f => f.Name == $"audioAnswers[{eq.QuestionId}]");

                    if (file != null && file.Length > 0)
                    {
                        // Tạo tên file
                        string uniqueFileName = $"q_{eq.QuestionId}_{Guid.NewGuid().ToString().Substring(0, 8)}.webm"; // Webm vì Chrome ghi format này
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);

                        // Lưu file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        // Lưu DB
                        string relativePath = Path.Combine("uploads", "student_exams", userId, Path.GetFileName(uploadFolder), uniqueFileName).Replace("\\", "/");
                        result.AudioAnswerUrl = "/" + relativePath;
                        result.IsCorrect = null;
                        hasManualGrading = true;
                    }
                    else
                    {
                        // Không có file hoặc file rỗng
                        result.IsCorrect = false;
                    }
                }
                else
                {
                    result.IsCorrect = false;
                }

                resultsList.Add(result);
            }

            attempt.Score = totalScore;
            attempt.TestResults = resultsList;

            if (hasManualGrading)
            {
                attempt.Status = (int)TestStatus.Submitted;
                attempt.TeacherFeedback = "Đang chờ chấm điểm phần Viết/Nói.";
            }

            _context.TestAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return RedirectToAction("Result", new { attemptId = attempt.Id });
        }

        public async Task<IActionResult> History()
        {
            // 1. Lấy ID người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var userId = "User_01"; // Mock tạm, nhớ thay bằng User thật khi chạy Identity

            // 2. Lấy danh sách các lần thi của user này
            var attempts = await _context.TestAttempts
                .Include(ta => ta.Exam) // Include để lấy tên đề thi
                .Where(ta => ta.UserId == userId)
                .OrderByDescending(ta => ta.StartTime) // Mới nhất lên đầu
                .ToListAsync();

            return View(attempts);
        }

        public async Task<IActionResult> Result(int attemptId)
        {
            var attempt = await _context.TestAttempts
                // 1. Load thông tin Đề thi và Cấu trúc đề
                .Include(ta => ta.Exam)
                    .ThenInclude(e => e.ExamParts)
                        .ThenInclude(ep => ep.ExamQuestions)

                // 2. Load thông tin User
                .Include(ta => ta.User)

                // 3. Load Kết quả làm bài
                .Include(ta => ta.TestResults)
                    .ThenInclude(tr => tr.Question)
                        .ThenInclude(q => q.Answers) // Collection con
                .Include(ta => ta.TestResults)
                    .ThenInclude(tr => tr.Question)
                        .ThenInclude(q => q.ReadingPassage)
                .Include(ta => ta.TestResults)
                    .ThenInclude(tr => tr.Question)
                        .ThenInclude(q => q.ListeningResource)

                // [QUAN TRỌNG] Thêm dòng này để tách query, sửa cảnh báo hiệu năng
                .AsSplitQuery()

                .FirstOrDefaultAsync(ta => ta.Id == attemptId);

            if (attempt == null) return NotFound();

            // Sắp xếp lại thứ tự phần thi (nên làm trong bộ nhớ sau khi load xong)
            if (attempt.Exam != null && attempt.Exam.ExamParts != null)
            {
                attempt.Exam.ExamParts = attempt.Exam.ExamParts.OrderBy(p => p.OrderIndex).ToList();
            }

            return View(attempt);
        }

    }
}