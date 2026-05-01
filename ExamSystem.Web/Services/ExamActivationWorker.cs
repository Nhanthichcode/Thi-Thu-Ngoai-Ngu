using ExamSystem.Infrastructure.Data; // Thay bằng namespace thực tế của bạn
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExamSystem.Web.Services
{
    public class ExamActivationWorker : BackgroundService
    {
        private readonly ILogger<ExamActivationWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ExamActivationWorker(ILogger<ExamActivationWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Dùng Console.WriteLine để ép buộc IN CHẮC CHẮN ra Terminal
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==================================================");
            Console.WriteLine("[WORKER] Dịch vụ tự động mở đề thi đã KHỞI ĐỘNG.");
            Console.WriteLine("==================================================");
            Console.ResetColor();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // Lấy giờ hiện tại (So sánh trực tiếp kiểu DateTime, bỏ qua mọi lỗi 12h/24h)
                        var now = DateTime.Now;

                        // LƯU Ý QUAN TRỌNG: Tuyệt đối không dùng .ToString() ở đây
                        // Dùng phép toán <= để nếu server có lag qua phút đó, đề thi vẫn được mở bù
                        var examsToActivate = await context.Exams
                            .Where(e => !e.IsActive && e.StartDate <= now)
                            .ToListAsync();

                        if (examsToActivate.Any())
                        {
                            foreach (var exam in examsToActivate)
                            {
                                exam.IsActive = true;
                            }

                            await context.SaveChangesAsync();
                            //TempData["SuccesMessage"] = "Có đề thi được mở lúc: " + now;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ép in lỗi ra màn hình đen bằng màu đỏ để dễ phát hiện
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Lỗi Worker: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[ERROR CHI TIẾT]: {ex.InnerException.Message}");
                    }
                    Console.ResetColor();
                }

                // Quét 1 phút 1 lần
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}