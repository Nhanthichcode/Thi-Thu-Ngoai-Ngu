using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using ExamSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] // 2. Thêm Attribute này
    [Authorize(Roles = "Admin, Teacher")]
    public class QuestionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public QuestionsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _webHostEnvironment = environment;
        }

        // ============================================================
        // 1. DANH SÁCH (INDEX)
        // ============================================================
        public async Task<IActionResult> Index(ExamSkill? skillType, int? level)
        {
            var query = _context.Questions
                .Include(q => q.ReadingPassage)
                .Include(q => q.ListeningResource)
                .OrderByDescending(q => q.CreatedDate)
                .AsQueryable();

            if (skillType.HasValue && skillType.Value != ExamSkill.None)
            {
                query = query.Where(q => q.SkillType == skillType.Value);
            }
            if (level.HasValue && level.Value > 0)
            {
                query = query.Where(q => q.Level == level.Value);
            }

            var list = await query.ToListAsync();
            var groupedList = new List<QuestionGroup>();

            // Nhóm Reading
            groupedList.AddRange(list.Where(q => q.ReadingPassageId.HasValue)
                .GroupBy(q => q.ReadingPassageId)
                .Select(g => new QuestionGroup
                {
                    GroupType = "Reading",
                    GroupId = g.Key,
                    GroupTitle = g.First().ReadingPassage.Title,
                    QuestionCount = g.Count(),
                    Questions = g.ToList()
                }));

            // Nhóm Listening
            groupedList.AddRange(list.Where(q => q.ListeningResourceId.HasValue && !q.ReadingPassageId.HasValue)
                .GroupBy(q => q.ListeningResourceId)
                .Select(g => new QuestionGroup
                {
                    GroupType = "Listening",
                    GroupId = g.Key,
                    GroupTitle = g.First().ListeningResource.Title,
                    QuestionCount = g.Count(),
                    Questions = g.ToList()
                }));

            // Nhóm Câu lẻ (Independent) - Mỗi câu 1 Group để hiển thị Card riêng
            foreach (var q in list.Where(q => !q.ReadingPassageId.HasValue && !q.ListeningResourceId.HasValue))
            {
                groupedList.Add(new QuestionGroup
                {
                    GroupType = q.SkillType.ToString(),
                    GroupTitle = q.Content,
                    QuestionCount = 1,
                    Questions = new List<Question> { q }
                });
            }
            ViewData["CurrentSkill"] = skillType ?? ExamSkill.None;
            ViewData["CurrentLevel"] = level ?? 0;

            // Sắp xếp ưu tiên Reading/Listening lên đầu
            return View(groupedList.OrderByDescending(g => g.GroupType == "Reading" || g.GroupType == "Listening")
                                   .ThenByDescending(g => g.Questions.FirstOrDefault()?.CreatedDate).ToList());
        }

        // ============================================================
        // 2. TẠO MỚI (CREATE)
        // ============================================================
        public IActionResult Create()
        {
            var model = new UnifiedCreateViewModel();
            model.Questions.Add(new QuestionItem()); // Dòng mặc định
            LoadDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UnifiedCreateViewModel model)
        {
            // --- 1. DỌN DẸP LỖI VALIDATE THEO TỪNG KỸ NĂNG ---
            ModelState.Remove("NewListeningFile");
            ModelState.Remove("CommonImageFile");

            // Nếu không phải môn ĐỌC, xóa lỗi của Đọc
            if (model.SkillType != ExamSkill.Reading)
            {
                ModelState.Remove("NewReadingTitle");
                ModelState.Remove("NewReadingContent");
                ModelState.Remove("ReadingPassageId");
            }

            // Nếu không phải môn NGHE, xóa lỗi của Nghe
            if (model.SkillType != ExamSkill.Listening)
            {
                ModelState.Remove("NewListeningTitle");
                ModelState.Remove("NewListeningTranscript");
                ModelState.Remove("ListeningResourceId");
            }

            // Nếu không phải môn NÓI, xóa lỗi của Nói
            if (model.SkillType != ExamSkill.Speaking)
            {
                ModelState.Remove("NewSpeakingTitle");
            }
           
            if (model.Questions != null)
            {
                for (int i = 0; i < model.Questions.Count; i++)
                {
                    // Giao diện ghi "Giải thích" là tùy chọn, nên mình xóa lỗi bắt buộc nhập của nó đi
                    ModelState.Remove($"Questions[{i}].Explaination");

                    // Nếu là môn Tự luận (Nói/Viết) thì KHÔNG CẦN đáp án A, B, C, D
                    if (model.SkillType == ExamSkill.Speaking || model.SkillType == ExamSkill.Writing)
                    {
                        ModelState.Remove($"Questions[{i}].AnswerA");
                        ModelState.Remove($"Questions[{i}].AnswerB");
                        ModelState.Remove($"Questions[{i}].AnswerC");
                        ModelState.Remove($"Questions[{i}].AnswerD");
                    }
                }
            }
            // --- 2. KIỂM TRA VALIDATE VÀ DEBUG ---
            if (!ModelState.IsValid)
            {
                LoadDropdowns(); // Load lại các list Bài nghe/Đọc
                return View(model); // Bật ngược lại màn hình cũ, kèm theo lỗi
            }

            // --- A. Xử lý Tạo Tài nguyên mới (Nếu có) ---
            if (model.SkillType == ExamSkill.Reading && !string.IsNullOrEmpty(model.NewReadingTitle))
            {
                var newPassage = new ReadingPassage { Title = model.NewReadingTitle, Content = model.NewReadingContent };
                _context.ReadingPassages.Add(newPassage);
                await _context.SaveChangesAsync();
                model.ReadingPassageId = newPassage.Id;
            }
            else if (model.SkillType == ExamSkill.Listening && model.NewListeningFile != null)
            {
                var audioUrl = await SaveFileAsync(model.NewListeningFile, "audio");
                var newResource = new ListeningResource
                {
                    Title = model.NewListeningTitle ?? "Audio " + DateTime.Now.Ticks,
                    AudioUrl = audioUrl,
                    Transcript = model.NewListeningTranscript
                };
                _context.ListeningResources.Add(newResource);
                await _context.SaveChangesAsync();
                model.ListeningResourceId = newResource.Id;
            }

            // --- B. Xử lý Ảnh chung (Cho Writing/Speaking) ---
            string? uploadedImageUrl = null;
            if (model.SkillType == ExamSkill.Speaking && model.CommonImageFile != null)
            {
                uploadedImageUrl = await SaveFileAsync(model.CommonImageFile, "images");
            }

            // --- C. Lưu Câu hỏi ---
            if (model.Questions != null && model.Questions.Any())
            {
                var questionsToAdd = new List<Question>();

                foreach (var item in model.Questions)
                {
                    if (string.IsNullOrWhiteSpace(item.Content)) continue;

                    string finalContent = item.Content;
                    // Ghép tiêu đề in đậm cho môn Nói
                    if (model.SkillType == ExamSkill.Speaking && !string.IsNullOrWhiteSpace(model.NewSpeakingTitle))
                    {
                        finalContent = $"{model.NewSpeakingTitle}";
                    }

                    var q = new Question
                    {                        
                        Content = item.Content,
                        Explaination = item.Explaination,
                        SkillType = model.SkillType,
                        QuestionType = (model.SkillType == ExamSkill.Writing) ? QuestionType.Essay :
                                       (model.SkillType == ExamSkill.Speaking) ? QuestionType.SpeakingRecording :
                                       QuestionType.SingleChoice,
                        Level = model.Level,
                        CreatedDate = DateTime.Now,
                        ReadingPassageId = (model.SkillType == ExamSkill.Reading) ? model.ReadingPassageId : null,
                        ListeningResourceId = (model.SkillType == ExamSkill.Listening) ? model.ListeningResourceId : null,
                        MediaUrl = uploadedImageUrl,
                        Answers = new List<Answer>()
                    };

                    // Thêm đáp án nếu là trắc nghiệm
                    if (model.SkillType != ExamSkill.Speaking && model.SkillType != ExamSkill.Writing)
                    {
                        q.Answers.Add(new Answer { Content = item.AnswerA ?? "", IsCorrect = (item.CorrectAnswerIndex == 0) });
                        q.Answers.Add(new Answer { Content = item.AnswerB ?? "", IsCorrect = (item.CorrectAnswerIndex == 1) });
                        q.Answers.Add(new Answer { Content = item.AnswerC ?? "", IsCorrect = (item.CorrectAnswerIndex == 2) });
                        q.Answers.Add(new Answer { Content = item.AnswerD ?? "", IsCorrect = (item.CorrectAnswerIndex == 3) });
                    }

                    questionsToAdd.Add(q);
                }

                if (questionsToAdd.Any())
                {
                    _context.Questions.AddRange(questionsToAdd);
                    await _context.SaveChangesAsync();
                }
            }

            // Form truyền thống thì phải dùng RedirectToAction
            return RedirectToAction(nameof(Index));
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(UnifiedCreateViewModel model)
        //{
        //    // Bỏ qua validate file vì xử lý thủ công
        //    ModelState.Remove("NewListeningFile");
        //    ModelState.Remove("CommonImageFile");
        //    ModelState.Remove("NewSpeakingTitle");

        //    if (!ModelState.IsValid)
        //    {
        //        LoadDropdowns();
        //        return View(model);
        //    }

        //    // A. Xử lý Tạo Tài nguyên mới (Nếu có)
        //    if (model.SkillType == ExamSkill.Reading && !string.IsNullOrEmpty(model.NewReadingTitle))
        //    {
        //        var newPassage = new ReadingPassage { Title = model.NewReadingTitle, Content = model.NewReadingContent };
        //        _context.ReadingPassages.Add(newPassage);
        //        await _context.SaveChangesAsync();
        //        model.ReadingPassageId = newPassage.Id;
        //    }
        //    else if (model.SkillType == ExamSkill.Listening && model.NewListeningFile != null)
        //    {
        //        var audioUrl = await SaveFileAsync(model.NewListeningFile, "audio");
        //        var newResource = new ListeningResource
        //        {
        //            Title = model.NewListeningTitle ?? "Audio " + DateTime.Now.Ticks,
        //            AudioUrl = audioUrl,
        //            Transcript = model.NewListeningTranscript
        //        };
        //        _context.ListeningResources.Add(newResource);
        //        await _context.SaveChangesAsync();
        //        model.ListeningResourceId = newResource.Id;
        //    }

        //    // B. Xử lý Ảnh chung (Cho Writing/Speaking)
        //    string? uploadedImageUrl = null;

        //    // CHỈ UPLOAD NẾU SKILL LÀ SPEAKING VÀ CÓ FILE ĐƯỢC CHỌN (KHÔNG BẮT BUỘC)
        //    if (model.SkillType == ExamSkill.Speaking)
        //    {
        //        // Chỉ chạy SaveFileAsync nếu người dùng thực sự chọn file
        //        if (model.CommonImageFile != null)
        //        {
        //            uploadedImageUrl = await SaveFileAsync(model.CommonImageFile, "images");
        //        }               
        //    }

        //    // C. Lưu Câu hỏi
        //    if (model.Questions != null && model.Questions.Any())
        //    {
        //        var questionsToAdd = new List<Question>();

        //        foreach (var item in model.Questions)
        //        {
        //            if (string.IsNullOrWhiteSpace(item.Content)) continue;

        //            string finalContent = item.Content;
        //            if (model.SkillType == ExamSkill.Speaking && !string.IsNullOrWhiteSpace(model.NewSpeakingTitle))
        //            {                        
        //                finalContent = $"<div class='fw-bold text-success mb-2' style='font-size: 1.1rem;'>{model.NewSpeakingTitle}</div>{item.Content}";
        //            }
        //            var q = new Question
        //            {
        //                Content = finalContent,
        //                Explaination = item.Explaination,
        //                SkillType = model.SkillType,
        //                QuestionType = (model.SkillType == ExamSkill.Writing) ? QuestionType.Essay :
        //                               (model.SkillType == ExamSkill.Speaking) ? QuestionType.SpeakingRecording :
        //                               QuestionType.SingleChoice,
        //                Level = model.Level,
        //                CreatedDate = DateTime.Now,
        //                ReadingPassageId = (model.SkillType == ExamSkill.Reading) ? model.ReadingPassageId : null,
        //                ListeningResourceId = (model.SkillType == ExamSkill.Listening) ? model.ListeningResourceId : null,
        //                MediaUrl = uploadedImageUrl,
        //                Answers = new List<Answer>()
        //            };

        //            // Thêm đáp án nếu là trắc nghiệm
        //            if (model.SkillType != ExamSkill.Speaking && model.SkillType != ExamSkill.Writing)
        //            {
        //                q.Answers.Add(new Answer { Content = item.AnswerA ?? "", IsCorrect = (item.CorrectAnswerIndex == 0) });
        //                q.Answers.Add(new Answer { Content = item.AnswerB ?? "", IsCorrect = (item.CorrectAnswerIndex == 1) });
        //                q.Answers.Add(new Answer { Content = item.AnswerC ?? "", IsCorrect = (item.CorrectAnswerIndex == 2) });
        //                q.Answers.Add(new Answer { Content = item.AnswerD ?? "", IsCorrect = (item.CorrectAnswerIndex == 3) });
        //            }

        //            questionsToAdd.Add(q);
        //        }

        //        if (questionsToAdd.Any())
        //        {
        //            _context.Questions.AddRange(questionsToAdd);
        //            await _context.SaveChangesAsync();
        //        }
        //    }

        //    return Json(new { isSuccess = true, redirectUrl = Url.Action("Index"), message = "Đã tạo bài mới thành công." });
        //}

        // ============================================================
        // 3. CHỈNH SỬA (EDIT) - Cho câu hỏi lẻ
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (question == null) return NotFound();

            // Đảm bảo đủ 4 dòng đáp án để view không lỗi
            if (question.Answers == null) question.Answers = new List<Answer>();
            while (question.Answers.Count < 4) question.Answers.Add(new Answer());

            LoadDropdowns(question);
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question question, int CorrectAnswerIndex, IFormFile? imageFile, string DeleteImageFlag)
        {
            if (id != question.Id) return NotFound();
            var existingQuestion = await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(x => x.Id == id);
            var dbQuestion = await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(x => x.Id == id);
            if (dbQuestion == null) return NotFound();

            ModelState.Remove("MediaUrl"); // Bỏ qua validate

            // A. Cập nhật thông tin chính
            dbQuestion.Content = question.Content;
            dbQuestion.Level = question.Level;
            dbQuestion.SkillType = question.SkillType;
            dbQuestion.ReadingPassageId = question.ReadingPassageId;
            dbQuestion.ListeningResourceId = question.ListeningResourceId;
            dbQuestion.Explaination = question.Explaination;

            // B. Xử lý Ảnh

            // Trường hợp 1: Người dùng bấm XÓA ẢNH CŨ
            if (DeleteImageFlag == "true")
            {
                if (!string.IsNullOrEmpty(existingQuestion.MediaUrl))
                {
                    // Xóa file vật lý
                    var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, existingQuestion.MediaUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }
                existingQuestion.MediaUrl = null; // Set DB field về NULL
            }

            // Trường hợp 2: Người dùng UPLOAD ẢNH MỚI
            if (imageFile != null && imageFile.Length > 0)
            {
                // Nếu đang có ảnh cũ (và chưa bị xóa ở bước 1) -> Xóa ảnh cũ
                if (!string.IsNullOrEmpty(existingQuestion.MediaUrl))
                {
                    var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, existingQuestion.MediaUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                // Upload ảnh mới và gán đường dẫn
                var fileName = DateTime.Now.Ticks + Path.GetExtension(imageFile.FileName);
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                existingQuestion.MediaUrl = "/uploads/images/" + fileName;
            }

            // C. Cập nhật Đáp án
            if (question.Answers != null && dbQuestion.Answers != null)
            {
                var formAnswers = question.Answers.ToList();
                var dbAnswers = dbQuestion.Answers.ToList();

                for (int i = 0; i < dbAnswers.Count; i++)
                {
                    if (i < formAnswers.Count) dbAnswers[i].Content = formAnswers[i].Content;
                    dbAnswers[i].IsCorrect = (i == CorrectAnswerIndex);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 4. BATCH EDIT (Sửa hàng loạt cho Bài Đọc/Nghe)
        // ============================================================
        public async Task<IActionResult> BatchEdit(int id, string type)
        {
            var model = new BatchEditViewModel { ResourceId = id, ResourceType = type };
            List<Question> questions = new List<Question>();

            if (type == "Reading")
            {
                var p = await _context.ReadingPassages.Include(x => x.Questions).ThenInclude(q => q.Answers).FirstOrDefaultAsync(x => x.Id == id);
                if (p == null) return NotFound();
                model.Title = p.Title; model.Content = p.Content; questions = p.Questions.ToList();
            }
            else
            {
                var r = await _context.ListeningResources.Include(x => x.Questions).ThenInclude(q => q.Answers).FirstOrDefaultAsync(x => x.Id == id);
                if (r == null) return NotFound();
                model.Title = r.Title; model.Content = r.Transcript; model.CurrentAudioUrl = r.AudioUrl; questions = r.Questions.ToList();
            }

            // Map sang ViewModel
            foreach (var q in questions)
            {
                var item = new QuestionEditItem
                {
                    Id = q.Id,
                    Content = q.Content,
                    Level = q.Level,
                    Explaination = q.Explaination,
                    MediaUrl = q.MediaUrl
                };
                if (q.Answers.Count >= 4)
                {
                    var ans = q.Answers.ToList();
                    item.AnswerA = ans[0].Content; item.AnswerB = ans[1].Content;
                    item.AnswerC = ans[2].Content; item.AnswerD = ans[3].Content;
                    for (int i = 0; i < 4; i++) if (ans[i].IsCorrect == true) item.CorrectAnswerIndex = i;
                }
                model.Questions.Add(item);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchEdit(BatchEditViewModel model)
        {
            // 1. Cập nhật Resource
            if (model.ResourceType == "Reading")
            {
                var p = await _context.ReadingPassages.FindAsync(model.ResourceId);
                if (p != null) { p.Title = model.Title; p.Content = model.Content; }
            }
            else
            {
                var r = await _context.ListeningResources.FindAsync(model.ResourceId);
                if (r != null)
                {
                    r.Title = model.Title; r.Transcript = model.Content;
                    if (model.NewAudioFile != null) r.AudioUrl = await SaveFileAsync(model.NewAudioFile, "audio");
                }
            }

            // 2. Xóa câu hỏi bị user xóa
            if (!string.IsNullOrEmpty(model.DeletedQuestionIds))
            {
                var ids = model.DeletedQuestionIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
                var toDelete = _context.Questions.Where(q => ids.Contains(q.Id));
                _context.Questions.RemoveRange(toDelete);
            }

            // 3. Cập nhật / Thêm câu hỏi
            foreach (var item in model.Questions)
            {
                Question q;
                if (item.Id == 0) // Thêm mới
                {
                    q = new Question
                    {
                        CreatedDate = DateTime.Now,
                        SkillType = model.ResourceType == "Reading" ? ExamSkill.Reading : ExamSkill.Listening,
                        ReadingPassageId = model.ResourceType == "Reading" ? model.ResourceId : null,
                        ListeningResourceId = model.ResourceType == "Listening" ? model.ResourceId : null,
                        Answers = new List<Answer> { new(), new(), new(), new() }
                    };
                    _context.Questions.Add(q);
                }
                else // Sửa
                {
                    q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == item.Id);
                }

                if (q != null)
                {
                    q.Content = item.Content; q.Level = item.Level; q.Explaination = item.Explaination;

                    var ansList = q.Answers.ToList();
                    // Đảm bảo đủ 4 đáp án
                    while (ansList.Count < 4) { var a = new Answer(); q.Answers.Add(a); ansList.Add(a); }

                    ansList[0].Content = item.AnswerA; ansList[0].IsCorrect = (item.CorrectAnswerIndex == 0);
                    ansList[1].Content = item.AnswerB; ansList[1].IsCorrect = (item.CorrectAnswerIndex == 1);
                    ansList[2].Content = item.AnswerC; ansList[2].IsCorrect = (item.CorrectAnswerIndex == 2);
                    ansList[3].Content = item.AnswerD; ansList[3].IsCorrect = (item.CorrectAnswerIndex == 3);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // ============================================================
        // 6. CÁC HÀM HỖ TRỢ (HELPER)
        // ============================================================

        // Helper: Lưu file vào thư mục wwwroot
        private async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            var fileName = DateTime.Now.Ticks + Path.GetExtension(file.FileName);
            var path = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/{folderName}/{fileName}";
        }

        // Helper: Load Dropdown cho View
        private void LoadDropdowns(Question? q = null)
        {
            ViewData["ReadingPassageId"] = new SelectList(_context.ReadingPassages, "Id", "Title", q?.ReadingPassageId);
            ViewData["ListeningResourceId"] = new SelectList(_context.ListeningResources, "Id", "Title", q?.ListeningResourceId);
        }

        // API: Lấy chi tiết Resource (Dùng cho AJAX ở trang Create)
        [HttpGet]
        public async Task<IActionResult> GetResourceDetails(int id, string type)
        {
            if (type == "reading")
            {
                var item = await _context.ReadingPassages.FindAsync(id);
                return item == null ? NotFound() : Json(new { success = true, content = item.Content });
            }
            if (type == "listening")
            {
                var item = await _context.ListeningResources.FindAsync(id);
                return item == null ? NotFound() : Json(new { success = true, audioUrl = item.AudioUrl, transcript = item.Transcript });
            }
            return BadRequest();
        }

        // API: Lấy danh sách câu hỏi khả dụng cho Exam (Dùng ở trang soạn đề thi)
        [HttpGet]
        public async Task<IActionResult> GetAvailableQuestions(int examId, string type, int? skillType)
        {
            var usedIds = await _context.ExamQuestions.Where(eq => eq.ExamPart.ExamId == examId).Select(eq => eq.QuestionId).ToListAsync();

            if (type == "Independent")
            {
                var query = _context.Questions.AsNoTracking().Where(q => q.ReadingPassageId == null && q.ListeningResourceId == null);
                if (skillType.HasValue) query = query.Where(q => q.SkillType == (ExamSkill)skillType.Value);

                var data = await query.Select(q => new
                {
                    q.Id,
                    Content = q.Content.Length > 100 ? q.Content.Substring(0, 100) + "..." : q.Content,
                    Skill = q.SkillType.ToString(),
                    q.Level,
                    IsSelected = usedIds.Contains(q.Id)
                }).ToListAsync();
                return Json(data);
            }
            // Logic cho Reading/Listening tương tự (Giữ nguyên như bạn đã viết)
            return BadRequest();
        }

        // Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var q = await _context.Questions.Include(q => q.ReadingPassage).FirstOrDefaultAsync(m => m.Id == id);
            return q == null ? NotFound() : View(q);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var q = await _context.Questions.FindAsync(id);
            if (q != null) { _context.Questions.Remove(q); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }


        // 2. GET: Tải file mẫu
        [HttpGet]
        public IActionResult DownloadTemplate(string type)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Data");

                // --- GỘP Ô HƯỚNG DẪN (A1:J5) ---
                ws.Cells["A1:J5"].Merge = true;
                ws.Cells["A1:J5"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                ws.Cells["A1:J5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                ws.Cells["A1:J5"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                ws.Cells["A1:J5"].Style.WrapText = true;

                // Mã định danh ô A6
                ws.Cells[6, 1].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                ws.Cells[6, 1].Style.Font.Bold = true;

                int headerRow = 7;
                int dataStartRow = 8;

                switch (type)
                {
                    case "Reading":
                        ws.Cells["A1"].Value = "HƯỚNG DẪN NHẬP LIỆU BÀI ĐỌC (READING):\n" +
                                               "- Dòng có Tiêu đề & Nội dung -> Bài mới.\n" +
                                               "- Dòng chỉ có Câu hỏi con -> Thuộc bài có tiêu đề ở trên nó.\n" +
                                               "- Cột 'Giải thích': Nhập lý do tại sao đáp án đó đúng.";
                        ws.Cells[6, 1].Value = "TYPE_READING";

                        ws.Cells[headerRow, 1].Value = "Tiêu đề Bài";
                        ws.Cells[headerRow, 2].Value = "Nội dung Bài";
                        ws.Cells[headerRow, 3].Value = "Câu hỏi con";
                        ws.Cells[headerRow, 4].Value = "Level (1-5)";
                        ws.Cells[headerRow, 5].Value = "Đáp án A";
                        ws.Cells[headerRow, 6].Value = "Đáp án B";
                        ws.Cells[headerRow, 7].Value = "Đáp án C";
                        ws.Cells[headerRow, 8].Value = "Đáp án D";
                        ws.Cells[headerRow, 9].Value = "Vị trí Đúng (1-4)";
                        ws.Cells[headerRow, 10].Value = "Giải thích đáp án";

                        // Mẫu
                        ws.Cells[dataStartRow, 1].Value = "Topic Environment";
                        ws.Cells[dataStartRow, 2].Value = "Full text...";
                        ws.Cells[dataStartRow, 3].Value = "Main idea?";
                        ws.Cells[dataStartRow, 4].Value = 2;
                        ws.Cells[dataStartRow, 5].Value = "A";
                        ws.Cells[dataStartRow, 6].Value = "B";
                        ws.Cells[dataStartRow, 9].Value = 1;
                        ws.Cells[dataStartRow, 10].Value = "Đáp án A đúng vì...";
                        break;

                    case "Listening":
                        ws.Cells["A1"].Value = "HƯỚNG DẪN NHẬP LIỆU BÀI NGHE (LISTENING):\n" +
                                               "- Dòng có Tiêu đề -> Bài nghe mới.\n" +
                                               "- Dòng chỉ có Câu hỏi con -> Thuộc bài có tiêu đề ở trên nó.\n" +
                                               "- Cột 'Giải thích': Nhập lý do chọn đáp án.";
                        ws.Cells[6, 1].Value = "TYPE_LISTENING";

                        ws.Cells[headerRow, 1].Value = "Tiêu đề";
                        ws.Cells[headerRow, 2].Value = "Transcript";
                        ws.Cells[headerRow, 3].Value = "Câu hỏi con";
                        ws.Cells[headerRow, 4].Value = "Level";
                        ws.Cells[headerRow, 5].Value = "A";
                        ws.Cells[headerRow, 6].Value = "B";
                        ws.Cells[headerRow, 7].Value = "C";
                        ws.Cells[headerRow, 8].Value = "D";
                        ws.Cells[headerRow, 9].Value = "Đúng (1-4)";
                        ws.Cells[headerRow, 10].Value = "Giải thích đáp án";
                        break;

                    case "Writing":
                        ws.Cells["A1"].Value = "HƯỚNG DẪN NHẬP LIỆU BÀI VIẾT (WRITING):\n" +
                                               "- Cột Bài mẫu: Gợi ý chi tiết hoặc bài văn mẫu.";
                        ws.Cells[6, 1].Value = "TYPE_WRITING";
                        ws.Cells[headerRow, 1].Value = "Đề bài";
                        ws.Cells[headerRow, 2].Value = "Gợi ý";
                        ws.Cells[headerRow, 3].Value = "Level";
                        ws.Cells[headerRow, 4].Value = "Bài mẫu / Giải thích chi tiết";
                        break;

                    case "Speaking":
                        ws.Cells["A1"].Value = "HƯỚNG DẪN NHẬP LIỆU BÀI NÓI (SPEAKING):\n" +
                                               "- Mỗi dòng là 1 câu hỏi/chủ đề nói độc lập.";
                        ws.Cells[6, 1].Value = "TYPE_SPEAKING";

                        ws.Cells[headerRow, 1].Value = "Chủ đề / Câu hỏi";
                        ws.Cells[headerRow, 2].Value = "Gợi ý các ý chính (Bullet points)";
                        ws.Cells[headerRow, 3].Value = "Level (1-5)";
                        ws.Cells[headerRow, 4].Value = "Bài nói mẫu (Sample Answer)";
                        break;

                    default: // Grammar
                        ws.Cells["A1"].Value = "HƯỚNG DẪN GRAMMAR:\n- Mỗi dòng là 1 câu hỏi độc lập.";
                        ws.Cells[6, 1].Value = "TYPE_GRAMMAR";

                        ws.Cells[headerRow, 1].Value = "Nội dung câu hỏi";
                        ws.Cells[headerRow, 2].Value = "Level";
                        ws.Cells[headerRow, 3].Value = "A";
                        ws.Cells[headerRow, 4].Value = "B";
                        ws.Cells[headerRow, 5].Value = "C";
                        ws.Cells[headerRow, 6].Value = "D";
                        ws.Cells[headerRow, 7].Value = "Đúng (1-4)";
                        ws.Cells[headerRow, 8].Value = "Giải thích đáp án";

                        ws.Cells[dataStartRow, 1].Value = "She ___ to school yesterday.";
                        ws.Cells[dataStartRow, 2].Value = 1;
                        ws.Cells[dataStartRow, 3].Value = "go";
                        ws.Cells[dataStartRow, 4].Value = "went";
                        ws.Cells[dataStartRow, 7].Value = 2;
                        ws.Cells[dataStartRow, 8].Value = "Quá khứ đơn";
                        break;
                }

                // Style Header
                using (var range = ws.Cells[headerRow, 1, headerRow, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                ws.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Mau_{type}.xlsx");
            }
        }

        // 1. GET: Hiển thị giao diện Import
        [HttpGet]
        public IActionResult Import()
        {
            return View(new ImportResultViewModel());
        }

        // 3. POST: Xử lý Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file, string mode, string selectedRows = "")
        {
            var result = new ImportResultViewModel();
            bool isSaveMode = mode == "save";

            if (file == null || file.Length <= 0)
            {
                result.IsSuccess = false;
                result.Message = "Vui lòng chọn file Excel.";
                return Json(result);
            }

            // --- ĐIỂM SỬA 1: Giải mã danh sách các dòng được tick từ giao diện ---
            List<int> selectedIndices = new List<int>();
            if (!string.IsNullOrEmpty(selectedRows))
            {
                selectedIndices = selectedRows.Split(',').Select(int.Parse).ToList();
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var ws = package.Workbook.Worksheets[0];

                    string typeCode = ws.Cells[6, 1].Text?.Trim();

                    if (string.IsNullOrEmpty(typeCode) || !typeCode.StartsWith("TYPE_"))
                    {
                        result.Errors.Add(new ImportError { Row = 6, ErrorMessage = "Ô A6 không chứa mã định danh hợp lệ (VD: TYPE_GRAMMAR)." });
                        return Json(result);
                    }

                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            int count = 0;
                            List<ImportError> errors = new List<ImportError>();

                            // --- ĐIỂM SỬA 2: Truyền selectedIndices xuống các hàm xử lý ---
                            switch (typeCode)
                            {
                                case "TYPE_READING": (count, errors) = await ProcessReading(ws, isSaveMode, selectedIndices); break;
                                case "TYPE_LISTENING": (count, errors) = await ProcessListening(ws, isSaveMode, selectedIndices); break;
                                case "TYPE_WRITING": (count, errors) = await ProcessWriting(ws, isSaveMode, selectedIndices); break;
                                case "TYPE_GRAMMAR": (count, errors) = await ProcessGrammar(ws, isSaveMode, selectedIndices); break;
                                case "TYPE_SPEAKING": (count, errors) = await ProcessSpeaking(ws, isSaveMode, selectedIndices); break;
                                default: errors.Add(new ImportError { Row = 6, ErrorMessage = $"Mã loại '{typeCode}' chưa được hỗ trợ." }); break;
                            }

                            result.ValidCount = count;
                            result.Errors = errors;
                            result.InvalidCount = errors.Count;

                            if (isSaveMode)
                            {
                                if (count == 0)
                                {
                                    result.IsSuccess = false;
                                    result.Message = "Không có dữ liệu hợp lệ nào được chọn để lưu.";
                                }
                                else
                                {
                                    await _context.SaveChangesAsync();
                                    await transaction.CommitAsync();

                                    string msg = $"Đã lưu thành công {count} câu hỏi!";
                                    return Json(new { isSuccess = true, redirectUrl = Url.Action("Index"), message = msg });
                                }
                            }
                            else
                            {
                                result.IsSuccess = !errors.Any() && count > 0;
                                if (result.IsSuccess)
                                    result.Message = $"Tuyệt vời! File hợp lệ ({count} mục). Bạn có thể bấm Nhập ngay.";
                                else
                                    result.Message = $"Kiểm tra hoàn tất: Tìm thấy {errors.Count} lỗi.";
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            result.Errors.Add(new ImportError { Row = 0, ErrorMessage = "Lỗi hệ thống: " + ex.Message });
                            result.IsSuccess = false;
                        }
                    }
                }
            }
            return Json(result);
        }

        // ============================================================
        // CÁC HÀM XỬ LÝ LOGIC CHI TIẾT (HELPER IMPORT)
        // Đã cập nhật chặn lưu theo danh sách Checkbox
        // ============================================================
        private async Task<(int count, List<ImportError> errors)> ProcessGrammar(ExcelWorksheet ws, bool save, List<int> selectedIndices)
        {
            var errors = new List<ImportError>();
            int count = 0;
            int rowCount = ws.Dimension.Rows;

            for (int row = 8; row <= rowCount; row++)
            {
                string content = ws.Cells[row, 1].Text?.Trim();

                if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(ws.Cells[row, 2].Text)) continue;

                if (string.IsNullOrEmpty(content))
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Nội dung câu hỏi không được để trống." });
                else if (content.Length < 5)
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Nội dung câu hỏi quá ngắn (tối thiểu 5 ký tự)." });

                int level = ws.Cells[row, 2].GetValue<int>();
                if (level < 1 || level > 5)
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Level phải từ 1-5." });

                int correctIdx = ws.Cells[row, 7].GetValue<int>();
                if (correctIdx < 1 || correctIdx > 4)
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Vị trí đáp án đúng phải là số từ 1-4." });

                int validAnswersCount = 0;
                bool isCorrectAnswerEmpty = false;

                for (int i = 0; i < 4; i++)
                {
                    string ansCheck = ws.Cells[row, 3 + i].Text?.Trim();
                    if (!string.IsNullOrEmpty(ansCheck)) validAnswersCount++;

                    if ((i + 1) == correctIdx && string.IsNullOrEmpty(ansCheck))
                        isCorrectAnswerEmpty = true;
                }

                if (validAnswersCount < 2)
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Câu hỏi phải có ít nhất 2 đáp án." });

                if (isCorrectAnswerEmpty)
                    errors.Add(new ImportError { Row = row, ErrorMessage = $"Bạn chọn đáp án đúng là vị trí {correctIdx} nhưng ô đáp án đó đang để trống." });

                if (errors.Any(e => e.Row == row)) continue;

                // --- ĐIỂM SỬA 3: Chặn lưu nếu không được tick ---
                bool isSelected = selectedIndices.Contains(row - 1); // JS đếm mảng từ 0, nên dòng 8 trong Excel = index 7

                if (save)
                {
                    if (isSelected)
                    {
                        string explanation = ws.Cells[row, 8].Text?.Trim();

                        var q = new Question
                        {
                            Content = content,
                            SkillType = ExamSkill.Grammar,
                            QuestionType = QuestionType.SingleChoice,
                            Level = level,
                            Explaination = explanation,
                            CreatedDate = DateTime.Now,
                            Answers = new List<Answer>()
                        };

                        for (int i = 0; i < 4; i++)
                        {
                            string ans = ws.Cells[row, 3 + i].Text?.Trim();
                            if (!string.IsNullOrEmpty(ans))
                            {
                                q.Answers.Add(new Answer { Content = ans, IsCorrect = (i + 1) == correctIdx });
                            }
                        }
                        _context.Questions.Add(q);
                        count++; // Chỉ tăng count khi thực sự lưu
                    }
                }
                else
                {
                    count++; // Ở chế độ check thử thì đếm hết
                }
            }
            return (count, errors);
        }

        private async Task<(int count, List<ImportError> errors)> ProcessReading(ExcelWorksheet ws, bool save, List<int> selectedIndices)
        {
            var errors = new List<ImportError>();
            int count = 0;
            int rowCount = ws.Dimension.Rows;
            ReadingPassage currentPassage = null;

            for (int row = 8; row <= rowCount; row++)
            {
                string title = ws.Cells[row, 1].Text?.Trim();
                string content = ws.Cells[row, 2].Text?.Trim();
                string qContent = ws.Cells[row, 3].Text?.Trim();

                bool hasTitle = !string.IsNullOrEmpty(title);
                bool hasContent = !string.IsNullOrEmpty(content);
                bool hasQuestion = !string.IsNullOrEmpty(qContent);

                if (!hasTitle && !hasContent && !hasQuestion)
                {
                    continue; // Bỏ qua dòng rác
                }

                bool isSelected = selectedIndices.Contains(row - 1);

                // TRƯỜNG HỢP A: ĐÂY LÀ DÒNG CÂU HỎI CON
                if (hasQuestion)
                {
                    if (currentPassage == null && save)
                    {
                        if (!hasTitle && !hasContent && isSelected)
                        {
                            if (!errors.Any(e => e.Row == row && e.ErrorMessage.Contains("THIẾU")))
                                errors.Add(new ImportError { Row = row, ErrorMessage = "Câu hỏi này không thuộc bài đọc hợp lệ nào." });
                        }
                    }

                    if (qContent.Length < 5)
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Nội dung câu hỏi quá ngắn (tối thiểu 5 ký tự)." });

                    int level = ws.Cells[row, 4].GetValue<int>();
                    int correctIdx = ws.Cells[row, 9].GetValue<int>();

                    if (level < 1 || level > 5)
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Level câu hỏi sai (1-5)." });

                    if (correctIdx < 1 || correctIdx > 4)
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Vị trí đáp án đúng sai (1-4)." });

                    int validAnsCount = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (!string.IsNullOrEmpty(ws.Cells[row, 5 + i].Text?.Trim())) validAnsCount++;
                    }

                    if (validAnsCount < 2)
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Câu hỏi trắc nghiệm phải có ít nhất 2 đáp án." });

                    if (correctIdx >= 1 && correctIdx <= 4)
                    {
                        if (string.IsNullOrEmpty(ws.Cells[row, 5 + (correctIdx - 1)].Text?.Trim()))
                            errors.Add(new ImportError { Row = row, ErrorMessage = $"Bạn chọn đáp án đúng là vị trí {correctIdx} nhưng ô đó lại rỗng." });
                    }

                    if (!errors.Any(e => e.Row == row))
                    {
                        if (save)
                        {
                            if (isSelected && currentPassage != null)
                            {
                                string explanation = ws.Cells[row, 10].Text?.Trim();
                                var q = new Question
                                {
                                    Content = qContent,
                                    SkillType = ExamSkill.Reading,
                                    QuestionType = QuestionType.SingleChoice,
                                    Level = level,
                                    Explaination = explanation,
                                    Answers = new List<Answer>()
                                };
                                for (int i = 0; i < 4; i++)
                                {
                                    string ans = ws.Cells[row, 5 + i].Text?.Trim();
                                    if (!string.IsNullOrEmpty(ans))
                                        q.Answers.Add(new Answer { Content = ans, IsCorrect = (i + 1) == correctIdx });
                                }
                                currentPassage.Questions.Add(q);
                                count++;
                            }
                        }
                        else { count++; }
                    }
                }
                // TRƯỜNG HỢP B: KHAI BÁO BÀI ĐỌC MỚI (CHA)
                else
                {
                    bool isPassageValid = true;

                    if (!hasTitle)
                    {
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Dòng khai báo bài đọc mới nhưng THIẾU TIÊU ĐỀ." });
                        isPassageValid = false;
                    }

                    if (!hasContent)
                    {
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Dòng khai báo bài đọc mới nhưng THIẾU NỘI DUNG." });
                        isPassageValid = false;
                    }
                    else if (content.Length < 10)
                    {
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Nội dung bài đọc quá ngắn (phải > 10 ký tự)." });
                        isPassageValid = false;
                    }

                    if (isPassageValid)
                    {
                        if (save && !errors.Any(e => e.Row == row))
                        {
                            if (isSelected)
                            {
                                currentPassage = new ReadingPassage
                                {
                                    Title = title,
                                    Content = content,
                                    Questions = new List<Question>()
                                };
                                _context.ReadingPassages.Add(currentPassage);
                            }
                            else
                            {
                                // Bị bỏ tick -> Vứt luôn bài đọc để các câu con bên dưới không có chỗ bấu víu
                                currentPassage = null;
                            }
                        }
                    }
                    else
                    {
                        currentPassage = null;
                    }
                }
            }
            return (count, errors);
        }

        private async Task<(int count, List<ImportError> errors)> ProcessListening(ExcelWorksheet ws, bool save, List<int> selectedIndices)
        {
            var errors = new List<ImportError>();
            int count = 0;
            int rowCount = ws.Dimension.Rows;
            ListeningResource currentResource = null;

            for (int row = 8; row <= rowCount; row++)
            {
                string title = ws.Cells[row, 1].Text?.Trim();
                string transcript = ws.Cells[row, 2].Text?.Trim();
                string qContent = ws.Cells[row, 3].Text?.Trim();

                bool hasTitle = !string.IsNullOrEmpty(title);
                bool hasTranscript = !string.IsNullOrEmpty(transcript);
                bool hasQuestion = !string.IsNullOrEmpty(qContent);

                if (!hasTitle && !hasTranscript && !hasQuestion) continue;

                bool isSelected = selectedIndices.Contains(row - 1);

                if (hasQuestion)
                {
                    if (currentResource == null && save)
                    {
                        if (!hasTitle && isSelected)
                            errors.Add(new ImportError { Row = row, ErrorMessage = "Câu hỏi này không thuộc bài nghe hợp lệ nào." });
                    }

                    if (qContent.Length < 5)
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Câu hỏi quá ngắn." });

                    int level = ws.Cells[row, 4].GetValue<int>();
                    int correctIdx = ws.Cells[row, 9].GetValue<int>();

                    if (level < 1 || level > 5) errors.Add(new ImportError { Row = row, ErrorMessage = "Level sai." });
                    if (correctIdx < 1 || correctIdx > 4) errors.Add(new ImportError { Row = row, ErrorMessage = "Vị trí đúng sai." });

                    int validAnsCount = 0;
                    for (int i = 0; i < 4; i++) if (!string.IsNullOrEmpty(ws.Cells[row, 5 + i].Text?.Trim())) validAnsCount++;

                    if (validAnsCount < 2) errors.Add(new ImportError { Row = row, ErrorMessage = "Thiếu đáp án." });

                    if (correctIdx >= 1 && correctIdx <= 4)
                    {
                        if (string.IsNullOrEmpty(ws.Cells[row, 5 + (correctIdx - 1)].Text?.Trim()))
                            errors.Add(new ImportError { Row = row, ErrorMessage = $"Đáp án đúng bị rỗng." });
                    }

                    if (!errors.Any(e => e.Row == row))
                    {
                        if (save)
                        {
                            if (isSelected && currentResource != null)
                            {
                                string explanation = ws.Cells[row, 10].Text?.Trim();
                                var q = new Question
                                {
                                    Content = qContent,
                                    SkillType = ExamSkill.Listening,
                                    QuestionType = QuestionType.SingleChoice,
                                    Level = level,
                                    Explaination = explanation,
                                    Answers = new List<Answer>()
                                };
                                for (int i = 0; i < 4; i++)
                                {
                                    string ans = ws.Cells[row, 5 + i].Text?.Trim();
                                    if (!string.IsNullOrEmpty(ans))
                                        q.Answers.Add(new Answer { Content = ans, IsCorrect = (i + 1) == correctIdx });
                                }
                                currentResource.Questions.Add(q);
                                count++;
                            }
                        }
                        else { count++; }
                    }
                }
                else
                {
                    bool isResourceValid = true;

                    if (!hasTitle)
                    {
                        errors.Add(new ImportError { Row = row, ErrorMessage = "Dòng khai báo bài nghe mới THIẾU TIÊU ĐỀ." });
                        isResourceValid = false;
                    }

                    if (isResourceValid)
                    {
                        if (save && !errors.Any(e => e.Row == row))
                        {
                            if (isSelected)
                            {
                                currentResource = new ListeningResource
                                {
                                    Title = title,
                                    Transcript = transcript,
                                    AudioUrl = "/uploads/audio/placeholder.mp3",
                                    Questions = new List<Question>()
                                };
                                _context.ListeningResources.Add(currentResource);
                            }
                            else
                            {
                                currentResource = null;
                            }
                        }
                    }
                    else
                    {
                        currentResource = null;
                    }
                }
            }
            return (count, errors);
        }

        private async Task<(int count, List<ImportError> errors)> ProcessWriting(ExcelWorksheet ws, bool save, List<int> selectedIndices)
        {
            var errors = new List<ImportError>();
            int count = 0;
            int rowCount = ws.Dimension.Rows;

            for (int row = 8; row <= rowCount; row++)
            {
                string content = ws.Cells[row, 1].Text?.Trim();

                if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(ws.Cells[row, 3].Text)) continue;

                if (string.IsNullOrEmpty(content))
                {
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Đề bài không được để trống." });
                }
                else if (content.Length < 10)
                {
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Đề bài quá ngắn (tối thiểu 10 ký tự)." });
                }

                int level = ws.Cells[row, 3].GetValue<int>();
                if (level < 1 || level > 5)
                {
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Level phải từ 1-5" });
                }

                if (errors.Any(e => e.Row == row)) continue;

                bool isSelected = selectedIndices.Contains(row - 1);

                if (save)
                {
                    if (isSelected)
                    {
                        string hint = ws.Cells[row, 2].Text?.Trim();
                        string explanation = ws.Cells[row, 4].Text?.Trim();

                        string finalContent = content + (string.IsNullOrEmpty(hint) ? "" : $"\n\n(Gợi ý: {hint})");

                        var q = new Question
                        {
                            Content = finalContent,
                            SkillType = ExamSkill.Writing,
                            QuestionType = QuestionType.Essay,
                            Level = level,
                            Explaination = explanation,
                            CreatedDate = DateTime.Now
                        };
                        _context.Questions.Add(q);
                        count++;
                    }
                }
                else { count++; }
            }
            return (count, errors);
        }

        private async Task<(int count, List<ImportError> errors)> ProcessSpeaking(ExcelWorksheet ws, bool save, List<int> selectedIndices)
        {
            var errors = new List<ImportError>();
            int count = 0;
            int rowCount = ws.Dimension.Rows;

            for (int row = 8; row <= rowCount; row++)
            {
                string content = ws.Cells[row, 1].Text?.Trim();

                if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(ws.Cells[row, 3].Text)) continue;

                if (string.IsNullOrEmpty(content))
                {
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Câu hỏi Speaking không được để trống." });
                }
                else if (content.Length < 5)
                {
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Nội dung quá ngắn." });
                }

                int level = ws.Cells[row, 3].GetValue<int>();
                if (level < 1 || level > 5)
                {
                    errors.Add(new ImportError { Row = row, ErrorMessage = "Level phải từ 1-5" });
                }

                if (errors.Any(e => e.Row == row)) continue;

                bool isSelected = selectedIndices.Contains(row - 1);

                if (save)
                {
                    if (isSelected)
                    {
                        string hint = ws.Cells[row, 2].Text?.Trim();
                        string sampleAnswer = ws.Cells[row, 4].Text?.Trim();

                        string finalContent = content + (string.IsNullOrEmpty(hint) ? "" : $"\n\n(Gợi ý: {hint})");

                        var q = new Question
                        {
                            Content = finalContent,
                            SkillType = ExamSkill.Speaking,
                            QuestionType = QuestionType.SpeakingRecording,
                            Level = level,
                            Explaination = sampleAnswer,
                            CreatedDate = DateTime.Now
                        };
                        _context.Questions.Add(q);
                        count++;
                    }
                }
                else { count++; }
            }
            return (count, errors);
        }

        // POST: Admin/Questions/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Không có câu hỏi nào được chọn để xóa." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Lấy danh sách câu hỏi cần xóa
                    var questionsToDelete = await _context.Questions
                        .Where(q => ids.Contains(q.Id))
                        .ToListAsync();

                    if (!questionsToDelete.Any())
                    {
                        return Json(new { success = false, message = "Không tìm thấy dữ liệu để xóa." });
                    }

                    // Tiến hành xóa
                    _context.Questions.RemoveRange(questionsToDelete);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = $"Đã xóa vĩnh viễn {questionsToDelete.Count} câu hỏi!" });
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