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
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Seed Roles & Users (Identity)
            await SeedIdentityAsync(userManager, roleManager);

            // 2. Seed Data (Questions & Structures)
            await SeedBusinessDataAsync(context);
        }

        private static async Task SeedIdentityAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await roleManager.RoleExistsAsync("Teacher"))
                await roleManager.CreateAsync(new IdentityRole("Teacher"));
            if (!await roleManager.RoleExistsAsync("Student"))
                await roleManager.CreateAsync(new IdentityRole("Student"));

            var adminEmail = "admin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new AppUser { UserName = adminEmail, Email = adminEmail, FullName = "Quản trị viên hệ thống", EmailConfirmed = true, DateOfBirth = new DateTime(1990, 1, 1) };
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            var teacherUser = await userManager.FindByEmailAsync("teacher@gmail.com");
            if (teacherUser == null)
            {
                var newTeacher = new AppUser { UserName = "teacher@gmail.com", Email = "teacher@gmail.com", FullName = "Cô Giáo Thảo", EmailConfirmed = true };
                var result = await userManager.CreateAsync(newTeacher, "Teacher@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newTeacher, "Teacher");
                    await userManager.AddToRoleAsync(newTeacher, "Author");
                }
            }

            var studentEmail = "student@example.com";
            if (await userManager.FindByEmailAsync(studentEmail) == null)
            {
                var studentUser = new AppUser { UserName = studentEmail, Email = studentEmail, FullName = "Nguyễn Văn Sinh Viên", EmailConfirmed = true, DateOfBirth = new DateTime(2000, 5, 15) };
                await userManager.CreateAsync(studentUser, "Student@123");
                await userManager.AddToRoleAsync(studentUser, "Student");
            }
        }

        private static async Task SeedBusinessDataAsync(AppDbContext context)
        {
            // Kiểm tra nếu đã có Câu hỏi thì không nạp thêm (Vì không tạo Đề nữa nên check bảng Câu hỏi)
            if (await context.Questions.AnyAsync()) return;

            // =========================================================================
            // 1. TẠO CẤU TRÚC ĐỀ THI CHUẨN (VSTEP, TOEIC, IELTS)
            // =========================================================================

            // --- CẤU TRÚC VSTEP ---
            var vstep = new ExamStructure { Name = "VSTEP (Tiêu chuẩn)", Description = "Cấu trúc 4 kỹ năng: Nghe, Đọc, Viết, Nói" };
            context.ExamStructures.Add(vstep);
            await context.SaveChangesAsync();

            var vstepParts = new List<StructurePart>
            {
                new StructurePart { ExamStructureId = vstep.Id, Name = "[LISTENING] Part 1: Hướng dẫn & Ví dụ", OrderIndex = 1, SkillType = ExamSkill.Listening },
                new StructurePart { ExamStructureId = vstep.Id, Name = "[READING] Part 1: Từ vựng & Ngữ pháp", OrderIndex = 2, SkillType = ExamSkill.Reading },
                new StructurePart { ExamStructureId = vstep.Id, Name = "[WRITING] Part 1: Viết thư/Email", OrderIndex = 3, SkillType = ExamSkill.Writing },
                new StructurePart { ExamStructureId = vstep.Id, Name = "[SPEAKING] Part 1: Tương tác xã hội", OrderIndex = 4, SkillType = ExamSkill.Speaking }
            };
            context.StructureParts.AddRange(vstepParts);
            await context.SaveChangesAsync();

            // --- CẤU TRÚC TOEIC ---
            var toeic = new ExamStructure { Name = "TOEIC (Listening & Reading)", Description = "Cấu trúc chuẩn 2 kỹ năng: Nghe và Đọc" };
            context.ExamStructures.Add(toeic);
            await context.SaveChangesAsync();

            var toeicParts = new List<StructurePart>
            {
                new StructurePart { ExamStructureId = toeic.Id, Name = "[LISTENING] Part 1: Photographs", OrderIndex = 1, SkillType = ExamSkill.Listening },
                new StructurePart { ExamStructureId = toeic.Id, Name = "[LISTENING] Part 2: Question-Response", OrderIndex = 2, SkillType = ExamSkill.Listening },
                new StructurePart { ExamStructureId = toeic.Id, Name = "[READING] Part 5: Incomplete Sentences", OrderIndex = 3, SkillType = ExamSkill.Reading },
                new StructurePart { ExamStructureId = toeic.Id, Name = "[READING] Part 6: Text Completion", OrderIndex = 4, SkillType = ExamSkill.Reading }
            };
            context.StructureParts.AddRange(toeicParts);
            await context.SaveChangesAsync();

            // --- CẤU TRÚC IELTS ---
            var ielts = new ExamStructure { Name = "IELTS (Academic)", Description = "Cấu trúc chuẩn 4 kỹ năng" };
            context.ExamStructures.Add(ielts);
            await context.SaveChangesAsync();

            var ieltsParts = new List<StructurePart>
            {
                new StructurePart { ExamStructureId = ielts.Id, Name = "[LISTENING] Part 1", OrderIndex = 1, SkillType = ExamSkill.Listening },
                new StructurePart { ExamStructureId = ielts.Id, Name = "[READING] Passage 1", OrderIndex = 2, SkillType = ExamSkill.Reading },
                new StructurePart { ExamStructureId = ielts.Id, Name = "[WRITING] Task 1", OrderIndex = 3, SkillType = ExamSkill.Writing },
                new StructurePart { ExamStructureId = ielts.Id, Name = "[SPEAKING] Part 1", OrderIndex = 4, SkillType = ExamSkill.Speaking }
            };
            context.StructureParts.AddRange(ieltsParts);
            await context.SaveChangesAsync();

            // =========================================================================
            // 2. SINH DỮ LIỆU NGÂN HÀNG CÂU HỎI (100 Grammar, 20 L/R/W/S)
            // =========================================================================
            var rnd = new Random();

            // A. TẠO 100 CÂU GRAMMAR
            for (int i = 1; i <= 100; i++)
            {
                var qGrammar = new Question
                {
                    Content = $"[Grammar {i}] This is an independent grammar question testing rule number {i}.",
                    SkillType = ExamSkill.Grammar,
                    Level = rnd.Next(1, 4),
                    CreatedDate = DateTime.Now,
                    Answers = new List<Answer>
                    {
                        new Answer { Content = "Option A", IsCorrect = false },
                        new Answer { Content = "Option B (Correct)", IsCorrect = true },
                        new Answer { Content = "Option C", IsCorrect = false },
                        new Answer { Content = "Option D", IsCorrect = false }
                    }
                };
                context.Questions.Add(qGrammar);
            }

            // B. TẠO 20 BÀI READING (Mỗi bài 5-10 câu con)
            var readingPassages = new List<ReadingPassage>();
            for (int i = 1; i <= 20; i++)
            {
                var rp = new ReadingPassage { Title = $"Reading Passage Topic {i}", Content = $"This is the full text content for reading passage {i}. It contains multiple paragraphs discussing various aspects of topic {i}." };
                context.ReadingPassages.Add(rp);
                readingPassages.Add(rp);
            }
            await context.SaveChangesAsync();

            foreach (var rp in readingPassages)
            {
                int numQs = rnd.Next(5, 11); // Random từ 5 đến 10 câu
                for (int j = 1; j <= numQs; j++)
                {
                    var q = new Question
                    {
                        Content = $"[Reading Q{j}] What is the main idea of paragraph {j} in this passage?",
                        SkillType = ExamSkill.Reading,
                        Level = rnd.Next(1, 4),
                        ReadingPassageId = rp.Id,
                        CreatedDate = DateTime.Now,
                        Answers = new List<Answer> { new Answer { Content = "A", IsCorrect = false }, new Answer { Content = "B", IsCorrect = true }, new Answer { Content = "C", IsCorrect = false }, new Answer { Content = "D", IsCorrect = false } }
                    };
                    context.Questions.Add(q);
                }
            }

            // C. TẠO 20 BÀI LISTENING (Mỗi bài 5-10 câu con)
            var listeningResources = new List<ListeningResource>();
            for (int i = 1; i <= 20; i++)
            {
                var lr = new ListeningResource { Title = $"Listening Section {i}", Transcript = $"Speaker A: Hello. Speaker B: Hi, welcome to section {i}." };
                context.ListeningResources.Add(lr);
                listeningResources.Add(lr);
            }
            await context.SaveChangesAsync();

            foreach (var lr in listeningResources)
            {
                int numQs = rnd.Next(5, 11);
                for (int j = 1; j <= numQs; j++)
                {
                    var q = new Question
                    {
                        Content = $"[Listening Q{j}] What does the speaker imply about topic {j}?",
                        SkillType = ExamSkill.Listening,
                        Level = rnd.Next(1, 4),
                        ListeningResourceId = lr.Id,
                        CreatedDate = DateTime.Now,
                        Answers = new List<Answer> { new Answer { Content = "A", IsCorrect = true }, new Answer { Content = "B", IsCorrect = false }, new Answer { Content = "C", IsCorrect = false }, new Answer { Content = "D", IsCorrect = false } }
                    };
                    context.Questions.Add(q);
                }
            }

            // D. TẠO 20 CÂU SPEAKING VÀ 20 CÂU WRITING
            for (int i = 1; i <= 20; i++)
            {
                var qSpeak = new Question { Content = $"[Speaking Task {i}] Describe a memorable event related to topic {i}.", SkillType = ExamSkill.Speaking, Level = 2, CreatedDate = DateTime.Now, Answers = new List<Answer>() };
                var qWrite = new Question { Content = $"[Writing Task {i}] Some people think X is better than Y. Discuss both views and give your opinion.", SkillType = ExamSkill.Writing, Level = 3, CreatedDate = DateTime.Now, Answers = new List<Answer>() };

                context.Questions.Add(qSpeak);
                context.Questions.Add(qWrite);
            }

            // Lưu toàn bộ Question vào DB
            await context.SaveChangesAsync();
        }
    }
}