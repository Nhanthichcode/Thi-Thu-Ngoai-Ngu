using ExamSystem.Infrastructure.Data; // Thay bằng namespace thực tế của bạn
using Microsoft.EntityFrameworkCore;
using ExamSystem.Core.Entities;

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
            _logger.LogInformation("Dịch vụ tự động mở đề thi đã khởi động.");

            // Vòng lặp vô tận cho đến khi ứng dụng tắt
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                       
                        var now = DateTime.Now;
                        var examsToActivate = await context.Exams
                            .Where(e => !e.IsActive && e.StartDate <= now)
                            .ToListAsync();

                        if (examsToActivate.Any())
                        {
                            foreach (var exam in examsToActivate)
                            {
                                exam.IsActive = true; // [cite: 88]
                                _logger.LogInformation($"Kích hoạt đề thi: {exam.Title} lúc {now}");
                            }

                            await context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi quét tự động mở đề thi.");
                }
              
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}