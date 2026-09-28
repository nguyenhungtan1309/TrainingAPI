using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Messaging
{
    public class ConversationCacheService : IConversationCacheService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan OperationTimeout = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(30);

        private readonly IDistributedCache _cache;
        private readonly ILogger<ConversationCacheService> _logger;

        private long _circuitOpenUntilTicks;

        public ConversationCacheService(IDistributedCache cache, ILogger<ConversationCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        private static string BuildKey(int viewerId, int top) => $"conversations:{viewerId}:top{top}";

        private bool IsCircuitOpen => DateTime.UtcNow.Ticks < Interlocked.Read(ref _circuitOpenUntilTicks);

        private void OpenCircuit(Exception ex, string operation, int viewerId)
        {
            Interlocked.Exchange(ref _circuitOpenUntilTicks, DateTime.UtcNow.Add(BreakDuration).Ticks);
            _logger.LogWarning(ex,
                "Redis không phản hồi khi {Operation} (User {ViewerId}). Tạm bỏ qua cache trong {Seconds} giây, request vẫn đọc thẳng từ database.",
                operation, viewerId, (int)BreakDuration.TotalSeconds);
        }

        public async Task<List<ConversationDTO>?> GetFirstPageAsync(int viewerId, int top, CancellationToken cancellationToken = default)
        {
            if (IsCircuitOpen) return null;

            try
            {
                var cachedJson = await _cache.GetStringAsync(BuildKey(viewerId, top), cancellationToken)
                    .WaitAsync(OperationTimeout, cancellationToken);

                if (cachedJson is null) return null;

                return JsonSerializer.Deserialize<List<ConversationDTO>>(cachedJson);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            catch (Exception ex)
            {
                OpenCircuit(ex, "đọc cache", viewerId);
                return null;
            }
        }

        public async Task SetFirstPageAsync(int viewerId, int top, List<ConversationDTO> data, CancellationToken cancellationToken = default)
        {
            if (IsCircuitOpen) return;

            try
            {
                var json = JsonSerializer.Serialize(data);
                await _cache.SetStringAsync(
                        BuildKey(viewerId, top),
                        json,
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                        cancellationToken)
                    .WaitAsync(OperationTimeout, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                OpenCircuit(ex, "ghi cache", viewerId);
            }
        }

        public async Task InvalidateAsync(int viewerId, CancellationToken cancellationToken = default)
        {
            if (IsCircuitOpen) return;

            try
            {
                await _cache.RemoveAsync(BuildKey(viewerId, 30), cancellationToken)
                    .WaitAsync(OperationTimeout, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                OpenCircuit(ex, "xóa cache", viewerId);
            }
        }
    }
}
