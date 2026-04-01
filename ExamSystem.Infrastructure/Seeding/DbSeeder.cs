using ExamSystem.Core.Entities;
using ExamSystem.Core.Enums;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSystem.Infrastructure.Seeding
{
    public static class DbSeeder
    {
        public static async Task SeedAllAsync(IServiceProvider serviceProvider)
        {
            // Lấy các dịch vụ cần thiết từ ServiceProvider
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Seed Roles & Users (Identity)
            await SeedIdentityAsync(userManager, roleManager);

            // 2. Seed Data (Exams, Questions...)
            await SeedBusinessDataAsync(context);
        }

        private static async Task SeedIdentityAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // --- TẠO ROLE ---
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("Teacher"))
                await roleManager.CreateAsync(new IdentityRole("Teacher"));

            if (!await roleManager.RoleExistsAsync("Student"))
                await roleManager.CreateAsync(new IdentityRole("Student"));

            // --- TẠO ADMIN ---
            var adminEmail = "admin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản trị viên hệ thống",
                    EmailConfirmed = true,
                    DateOfBirth = new DateTime(1990, 1, 1),
                };
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // --- TẠO TEACHER ---
            var teacherUser = await userManager.FindByEmailAsync("teacher@gmail.com");
            if (teacherUser == null)
            {
                var newTeacher = new AppUser
                {
                    UserName = "teacher@gmail.com",
                    Email = "teacher@gmail.com",
                    FullName = "Cô Giáo Thảo",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(newTeacher, "Teacher@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newTeacher, "Teacher");
                    await userManager.AddToRoleAsync(newTeacher, "Author");
                }
            }

            // --- TẠO STUDENT ---
            var studentEmail = "student@example.com";
            if (await userManager.FindByEmailAsync(studentEmail) == null)
            {
                var studentUser = new AppUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    FullName = "Nguyễn Văn Sinh Viên",
                    EmailConfirmed = true,
                    DateOfBirth = new DateTime(2000, 5, 15),
                };
                await userManager.CreateAsync(studentUser, "Student@123");
                await userManager.AddToRoleAsync(studentUser, "Student");
            }
        }

        private static async Task SeedBusinessDataAsync(AppDbContext context)
        {
            // Kiểm tra nếu đã có Exam thì không nạp thêm (tránh trùng lặp)
            if (await context.Exams.AnyAsync()) return;

            // =========================================================================
            // 1. VÒNG LẶP TẠO 10 ĐỀ THI VÀ CÂU HỎI ĐỘC LẬP
            // =========================================================================
            for (int i = 1; i <= 10; i++)
            {
                // A. TẠO TÀI NGUYÊN (READING)
                var readingPassage = new ReadingPassage
                {
                    Title = $"Bài đọc cho Đề {i}",
                    Content = $"Nội dung bài đọc số {i}. Early rising is a good habit. It leads to health and happiness..."
                };
                context.ReadingPassages.Add(readingPassage);
                await context.SaveChangesAsync(); // Lưu để lấy ID bài đọc

                // B. TẠO CÂU HỎI (5 KỸ NĂNG)
                var questions = new List<Question>();

                // Kỹ năng GRAMMAR
                var qGrammar = new Question
                {
                    Content = $"[Ngữ pháp câu {i}] She ______ to the market yesterday.",
                    SkillType = ExamSkill.Grammar,
                    Level = 1,
                    CreatedDate = DateTime.Now,
                    Answers = new List<Answer>
                    {
                        new Answer { Content = "go", IsCorrect = false },
                        new Answer { Content = "went", IsCorrect = true },
                        new Answer { Content = "gone", IsCorrect = false },
                        new Answer { Content = "goes", IsCorrect = false }
                    }
                };
                questions.Add(qGrammar);

                // Kỹ năng READING
                var qReading = new Question
                {
                    Content = $"[Đọc hiểu câu {i}] According to passage {i}, what does early rising lead to?",
                    SkillType = ExamSkill.Reading,
                    Level = 1,
                    ReadingPassageId = readingPassage.Id,
                    CreatedDate = DateTime.Now,
                    Answers = new List<Answer>
                    {
                        new Answer { Content = "Health and happiness", IsCorrect = true },
                        new Answer { Content = "Wealth and power", IsCorrect = false },
                        new Answer { Content = "Stress and anxiety", IsCorrect = false },
                        new Answer { Content = "Nothing special", IsCorrect = false }
                    }
                };
                questions.Add(qReading);

                // Kỹ năng LISTENING (Không dùng AudioUrl)
                var qListening = new Question
                {
                    Content = $"[Nghe câu {i}] What time does Sarah wake up in this scenario?",
                    SkillType = ExamSkill.Listening,
                    Level = 2,
                    CreatedDate = DateTime.Now,
                    Answers = new List<Answer>
                    {
                        new Answer { Content = "5 AM", IsCorrect = false },
                        new Answer { Content = "6 AM", IsCorrect = true },
                        new Answer { Content = "7 AM", IsCorrect = false },
                        new Answer { Content = "8 AM", IsCorrect = false }
                    }
                };
                questions.Add(qListening);

                // Kỹ năng SPEAKING (Không dùng MediaUrl)
                var qSpeaking = new Question
                {
                    Content = $"[Nói câu {i}] Talk about your favorite daily activity and explain why you like it.",
                    SkillType = ExamSkill.Speaking,
                    Level = 3,
                    CreatedDate = DateTime.Now,
                    Answers = new List<Answer>()
                };
                questions.Add(qSpeaking);

                // Kỹ năng WRITING
                var qWriting = new Question
                {
                    Content = $"[Viết câu {i}] Write an essay about the advantages of public transport.",
                    SkillType = ExamSkill.Writing,
                    Level = 3,
                    CreatedDate = DateTime.Now,
                    Answers = new List<Answer>()
                };
                questions.Add(qWriting);

                context.Questions.AddRange(questions);
                await context.SaveChangesAsync();

                // C. TẠO ĐỀ THI (EXAM) & CẤU TRÚC (EXAM PARTS)
                var exam = new Exam
                {
                    Title = $"Đề thi thử Full Skills Số {i}",
                    Description = $"Đề số {i} bao gồm Ngữ pháp, Đọc, Nghe, Nói và Viết.",
                    DurationMinutes = 60,
                    StartDate = DateTime.Now,
                    IsActive = true,
                };
                context.Exams.Add(exam);
                await context.SaveChangesAsync();

                var part1 = new ExamPart { ExamId = exam.Id, Name = "Part 1: Grammar & Reading", OrderIndex = 1 };
                var part2 = new ExamPart { ExamId = exam.Id, Name = "Part 2: Listening", OrderIndex = 2 };
                var part3 = new ExamPart { ExamId = exam.Id, Name = "Part 3: Speaking & Writing", OrderIndex = 3 };

                context.ExamParts.AddRange(part1, part2, part3);
                await context.SaveChangesAsync();

                // D. GÁN CÂU HỎI VÀO ĐỀ THI
                var examQuestions = new List<ExamQuestion>
                {
                    new ExamQuestion { ExamPartId = part1.Id, QuestionId = qGrammar.Id, Score = 10, SortOrder = 1 },
                    new ExamQuestion { ExamPartId = part1.Id, QuestionId = qReading.Id, Score = 20, SortOrder = 2 },
                    new ExamQuestion { ExamPartId = part2.Id, QuestionId = qListening.Id, Score = 20, SortOrder = 1 },
                    new ExamQuestion { ExamPartId = part3.Id, QuestionId = qSpeaking.Id, Score = 25, SortOrder = 1 },
                    new ExamQuestion { ExamPartId = part3.Id, QuestionId = qWriting.Id, Score = 25, SortOrder = 2 }
                };

                context.ExamQuestions.AddRange(examQuestions);
                await context.SaveChangesAsync();

                // E. TẠO LƯỢT THI MẪU CHO ĐỀ SỐ 1 (Để test UI)
                if (i == 1)
                {
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "student@example.com");
                    if (user != null)
                    {
                        var attempt = new TestAttempt
                        {
                            ExamId = exam.Id,
                            UserId = user.Id,
                            StartTime = DateTime.Now.AddHours(-2),
                            SubmitTime = DateTime.Now.AddHours(-1),
                            Status = (int)TestStatus.Graded,
                            Score = 75
                        };
                        context.TestAttempts.Add(attempt);
                        await context.SaveChangesAsync();
                    }
                }
            } // Hết vòng lặp 10 đề


            // =========================================================================
            // 2. TẠO CẤU TRÚC ĐỀ THI CHUẨN (VSTEP, TOEIC, IELTS)
            // =========================================================================
            
            // --- CẤU TRÚC VSTEP ---
            var vstep = new ExamStructure { Name = "VSTEP (Tiêu chuẩn)", Description = "Cấu trúc 4 kỹ năng: Nghe (4 parts), Đọc (4 parts), Viết (2 parts), Nói (2 parts)" };
            context.ExamStructures.Add(vstep);
            await context.SaveChangesAsync();

            var vstepParts = new List<StructurePart>();
            int order = 1;
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 1: Hướng dẫn & Ví dụ", OrderIndex = order++, Description = "Nghe thông báo, hướng dẫn ngắn", SkillType = ExamSkill.Listening });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 2: Hội thoại", OrderIndex = order++, Description = "Nghe hội thoại và trả lời câu hỏi", SkillType = ExamSkill.Listening });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 3: Bài nói chuyện/Bài giảng", OrderIndex = order++, Description = "Nghe bài giảng dài", SkillType = ExamSkill.Listening });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 4: Phỏng vấn/Thảo luận", OrderIndex = order++, Description = "Nghe phỏng vấn phức tạp", SkillType = ExamSkill.Listening });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 1: Từ vựng & Ngữ pháp", OrderIndex = order++, Description = "Điền từ vào chỗ trống", SkillType = ExamSkill.Reading });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 2: Đọc biển báo/Thông báo", OrderIndex = order++, Description = "Hiểu ý chính thông báo", SkillType = ExamSkill.Reading });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 3: Đọc hiểu văn bản", OrderIndex = order++, Description = "Đọc đoạn văn và trả lời", SkillType = ExamSkill.Reading });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 4: Đọc hiểu nâng cao", OrderIndex = order++, Description = "Đọc bài báo/tạp chí chuyên sâu", SkillType = ExamSkill.Reading });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[WRITING] Part 1: Viết thư/Email", OrderIndex = order++, Description = "Viết một bức thư khoảng 120 từ", SkillType = ExamSkill.Writing });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[WRITING] Part 2: Viết luận (Essay)", OrderIndex = order++, Description = "Viết bài luận khoảng 250 từ", SkillType = ExamSkill.Writing });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[SPEAKING] Part 1: Tương tác xã hội", OrderIndex = order++, Description = "Trả lời câu hỏi về bản thân", SkillType = ExamSkill.Speaking });
            vstepParts.Add(new StructurePart { ExamStructureId = vstep.Id, Name = "[SPEAKING] Part 2: Thảo luận giải pháp/Phát triển chủ đề", OrderIndex = order++, Description = "Thảo luận và đưa ra ý kiến", SkillType = ExamSkill.Speaking });
            context.StructureParts.AddRange(vstepParts);
            await context.SaveChangesAsync();

            // --- CẤU TRÚC TOEIC ---
            var toeic = new ExamStructure { Name = "TOEIC (Listening & Reading)", Description = "Cấu trúc chuẩn 2 kỹ năng: Nghe (4 phần, 100 câu, 45 phút) và Đọc (3 phần, 100 câu, 75 phút)." };
            context.ExamStructures.Add(toeic);
            await context.SaveChangesAsync();

            var toeicParts = new List<StructurePart>();
            int toeicOrder = 1;
            toeicParts.Add(new StructurePart { ExamStructureId = toeic.Id, Name = "[LISTENING] Part 1: Photographs", OrderIndex = toeicOrder++, Description = "Mô tả tranh (6 câu)", SkillType = ExamSkill.Listening });
            toeicParts.Add(new StructurePart { ExamStructureId = toeic.Id, Name = "[LISTENING] Part 2: Question-Response", OrderIndex = toeicOrder++, Description = "Hỏi - Đáp (25 câu)", SkillType = ExamSkill.Listening });
            toeicParts.Add(new StructurePart { ExamStructureId = toeic.Id, Name = "[LISTENING] Part 3: Conversations", OrderIndex = toeicOrder++, Description = "Hội thoại ngắn - 13 đoạn (39 câu)", SkillType = ExamSkill.Listening });
            toeicParts.Add(new StructurePart { ExamStructureId = toeic.Id, Name = "[LISTENING] Part 4: Talks", OrderIndex = toeicOrder++, Description = "Bài nói ngắn - 10 đoạn (30 câu)", SkillType = ExamSkill.Listening });
            toeicParts.Add(new StructurePart { ExamStructureId = toeic.Id, Name = "[READING] Part 5: Incomplete Sentences", OrderIndex = toeicOrder++, Description = "Hoàn thành câu (30 câu)", SkillType = ExamSkill.Reading });
            toeicParts.Add(new StructurePart { ExamStructureId = toeic.Id, Name = "[READING] Part 6: Text Completion", OrderIndex = toeicOrder++, Description = "Hoàn thành đoạn văn (16 câu)", SkillType = ExamSkill.Reading });
            toeicParts.Add(new StructurePart { ExamStructureId = toeic.Id, Name = "[READING] Part 7: Reading Comprehension", OrderIndex = toeicOrder++, Description = "Đọc hiểu đoạn văn đơn & kép/ba (54 câu)", SkillType = ExamSkill.Reading });
            context.StructureParts.AddRange(toeicParts);
            await context.SaveChangesAsync();

            // --- CẤU TRÚC IELTS ---
            var ielts = new ExamStructure { Name = "IELTS (Academic)", Description = "Cấu trúc chuẩn 4 kỹ năng: Nghe (4 phần, 30 phút), Đọc (3 phần, 60 phút), Viết (2 phần, 60 phút), Nói (3 phần, 11-15 phút)." };
            context.ExamStructures.Add(ielts);
            await context.SaveChangesAsync();

            var ieltsParts = new List<StructurePart>();
            int ieltsOrder = 1;
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[LISTENING] Part 1", OrderIndex = ieltsOrder++, Description = "10 câu hỏi", SkillType = ExamSkill.Listening });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[LISTENING] Part 2", OrderIndex = ieltsOrder++, Description = "10 câu hỏi", SkillType = ExamSkill.Listening });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[LISTENING] Part 3", OrderIndex = ieltsOrder++, Description = "10 câu hỏi", SkillType = ExamSkill.Listening });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[LISTENING] Part 4", OrderIndex = ieltsOrder++, Description = "10 câu hỏi", SkillType = ExamSkill.Listening });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[READING] Passage 1", OrderIndex = ieltsOrder++, Description = "Bài đọc 1", SkillType = ExamSkill.Reading });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[READING] Passage 2", OrderIndex = ieltsOrder++, Description = "Bài đọc 2", SkillType = ExamSkill.Reading });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[READING] Passage 3", OrderIndex = ieltsOrder++, Description = "Bài đọc 3", SkillType = ExamSkill.Reading });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[WRITING] Task 1", OrderIndex = ieltsOrder++, Description = "Tối thiểu 150 từ", SkillType = ExamSkill.Writing });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[WRITING] Task 2", OrderIndex = ieltsOrder++, Description = "Tối thiểu 250 từ", SkillType = ExamSkill.Writing });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[SPEAKING] Part 1", OrderIndex = ieltsOrder++, Description = "Giới thiệu & Phỏng vấn ngắn", SkillType = ExamSkill.Speaking });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[SPEAKING] Part 2", OrderIndex = ieltsOrder++, Description = "Trình bày cá nhân (Long turn)", SkillType = ExamSkill.Speaking });
            ieltsParts.Add(new StructurePart { ExamStructureId = ielts.Id, Name = "[SPEAKING] Part 3", OrderIndex = ieltsOrder++, Description = "Thảo luận", SkillType = ExamSkill.Speaking });
            context.StructureParts.AddRange(ieltsParts);
            await context.SaveChangesAsync();
        }
    }
}