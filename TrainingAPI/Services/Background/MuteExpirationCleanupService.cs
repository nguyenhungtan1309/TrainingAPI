using TrainingAPI.Services.Common;

namespace TrainingAPI.Services.Background
{
    public class MuteExpirationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MuteExpirationCleanupService> _logger;
        private readonly TimeSpan _interval;

        public MuteExpirationCleanupService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<MuteExpirationCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var seconds = configuration.GetValue("BackgroundJobs:MuteCleanupIntervalSeconds", 60);
            _interval = TimeSpan.FromSeconds(Math.Max(seconds, 5));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MuteExpirationCleanupService khởi động, chu kỳ {Interval}.", _interval);

            using var timer = new PeriodicTimer(_interval);
            try
            {
                do
                {
                    await CleanupOnceAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
            }

            _logger.LogInformation("MuteExpirationCleanupService đã dừng.");
        }

        private async Task CleanupOnceAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ISqlDataAccess>();

                var affected = await db.ExecuteRawNonQueryAsync(
                    @"UPDATE dbo.ThreadParticipant
                      SET IsMuted = 0, MutedUntilUTC = NULL
                      WHERE IsMuted = 1 AND MutedUntilUTC IS NOT NULL AND MutedUntilUTC <= SYSUTCDATETIME();",
                    null,
                    cancellationToken);

                if (affected > 0)
                    _logger.LogInformation("Đã bỏ tắt thông báo hết hạn cho {Count} thành viên hội thoại.", affected);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi dọn cờ tắt thông báo hết hạn, sẽ thử lại ở chu kỳ sau.");
            }
        }
    }
}
