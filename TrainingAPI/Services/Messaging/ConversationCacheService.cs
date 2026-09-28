using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Messaging
{
    /// <summary>
    /// [3. Redis] Hiện thực Cache-Aside cho danh sách hội thoại:
    ///   1. ChatThreadService.GetConversationsAsync gọi GetFirstPageAsync trước
    ///      -> nếu có (cache hit) thì trả luôn, KHÔNG đụng SQL Server.
    ///   2. Nếu không có (cache miss), ChatThreadService tự đọc DB như cũ,
    ///      rồi gọi SetFirstPageAsync để lưu lại cho lần sau.
    ///   3. ChatMessageService gọi InvalidateAsync cho từng thành viên của
    ///      thread ngay sau khi gửi tin thành công, để họ thấy hội thoại
    ///      nhảy lên đầu danh sách ngay lập tức thay vì phải chờ TTL hết hạn.
    ///
    /// CHỈ CACHE "TRANG ĐẦU" (beforeTimeUTC = null): đây là request được gọi
    /// lại nhiều nhất (mỗi lần mở app/refresh danh sách hội thoại - đúng
    /// cách chat.html đang gọi, luôn không kèm beforeTimeUTC). Các trang sau
    /// khi cuộn xuống gần như không lặp lại giữa các lần gọi, cache cho
    /// chúng sẽ toàn bộ là miss trong khi vẫn tốn round-trip tới Redis -
    /// không đáng, nên ChatThreadService sẽ bỏ qua cache hoàn toàn khi có
    /// beforeTimeUTC.
    ///
    /// TTL 20 GIÂY: đủ ngắn để dữ liệu không "cũ" quá lâu nếu vì lý do nào
    /// đó InvalidateAsync bị bỏ lỡ (ví dụ Redis tạm mất kết nối lúc đó),
    /// đồng thời đủ để giảm tải SQL Server trong các lần refresh liên tiếp
    /// (ví dụ người dùng bấm qua lại giữa các tab trong vài giây).
    ///
    /// AN TOÀN KHI REDIS LỖI: mọi thao tác đọc/ghi/xóa cache đều bọc
    /// try/catch và chỉ log Warning - Redis chết không được phép làm sập
    /// luồng nghiệp vụ chính (đúng nguyên tắc "Redis không nên là nguồn sự
    /// thật duy nhất" đã phân tích ở phần lý thuyết Redis).
    /// </summary>
    public class ConversationCacheService : IConversationCacheService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(20);

        private readonly IDistributedCache _cache;
        private readonly ILogger<ConversationCacheService> _logger;

        public ConversationCacheService(IDistributedCache cache, ILogger<ConversationCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        private static string BuildKey(int viewerId, int top) => $"conversations:{viewerId}:top{top}";

        public async Task<List<ConversationDTO>?> GetFirstPageAsync(int viewerId, int top, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedJson = await _cache.GetStringAsync(BuildKey(viewerId, top), cancellationToken);
                if (cachedJson is null)
                {
                    return null; // cache miss - bình thường, không phải lỗi
                }

                _logger.LogInformation("Cache HIT danh sách hội thoại (Redis) cho User {ViewerId}", viewerId);
                return JsonSerializer.Deserialize<List<ConversationDTO>>(cachedJson);
            }
            catch (Exception ex)
            {
                // Redis đang lỗi/không kết nối được KHÔNG được làm sập request -
                // coi như cache miss, để ChatThreadService rơi về đọc thẳng SQL Server.
                _logger.LogWarning(ex, "Không đọc được cache Redis cho danh sách hội thoại của User {ViewerId}, sẽ đọc thẳng từ database.", viewerId);
                return null;
            }
        }

        public async Task SetFirstPageAsync(int viewerId, int top, List<ConversationDTO> data, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                await _cache.SetStringAsync(
                    BuildKey(viewerId, top),
                    json,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Ghi cache thất bại không nghiêm trọng - lần đọc sau sẽ lại cache miss
                // và tự query DB, không mất dữ liệu, chỉ mất phần "tăng tốc".
                _logger.LogWarning(ex, "Không ghi được cache Redis cho danh sách hội thoại của User {ViewerId}.", viewerId);
            }
        }

        public async Task InvalidateAsync(int viewerId, CancellationToken cancellationToken = default)
        {
            try
            {
                // chat.html hiện luôn gọi API với top mặc định (30) - chỉ có đúng 1
                // biến thể key thực tế cần xóa. Nếu sau này có nhiều giá trị "top"
                // khác nhau được dùng thật, nên đổi sang lưu thêm 1 Set các key đã
                // cache theo từng user để xóa hết một lượt, thay vì đoán từng giá trị.
                await _cache.RemoveAsync(BuildKey(viewerId, 30), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không xóa được cache Redis danh sách hội thoại của User {ViewerId}.", viewerId);
            }
        }
    }
}
