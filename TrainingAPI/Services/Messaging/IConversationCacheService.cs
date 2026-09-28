using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Messaging
{
    /// <summary>
    /// [3. Redis][Cache-Aside] Hợp đồng cho việc cache danh sách hội thoại
    /// (kết quả của sp_GetConversationListV2) trong Redis.
    ///
    /// VÌ SAO TÁCH RIÊNG THÀNH SERVICE NÀY (không nhét thẳng IDistributedCache
    /// vào ChatThreadService): việc gửi tin (ChatMessageService) cũng cần
    /// XÓA cache này ngay sau khi gửi thành công (vì LastMessage/thứ tự
    /// hội thoại vừa đổi) - nếu ChatMessageService inject thẳng
    /// IChatThreadService để gọi lại, trong khi ChatThreadService lại đang
    /// inject IChatMessageService (để dùng ở GetThreadDetailsAsync), sẽ
    /// tạo ra phụ thuộc VÒNG giữa 2 service (A cần B, B cần A) - ASP.NET
    /// Core DI container sẽ ném lỗi ngay lúc khởi động. Tách một service
    /// nhỏ, không phụ thuộc ngược lại service nào khác, để cả hai bên
    /// cùng dùng chung mà không đụng nhau.
    /// </summary>
    public interface IConversationCacheService
    {
        /// <summary>Đọc cache "trang đầu" (không phân trang) danh sách hội thoại của 1 user. Trả về null nếu cache miss.</summary>
        Task<List<ConversationDTO>?> GetFirstPageAsync(int viewerId, int top, CancellationToken cancellationToken = default);

        /// <summary>Ghi cache "trang đầu" vừa đọc được từ DB, kèm TTL ngắn.</summary>
        Task SetFirstPageAsync(int viewerId, int top, List<ConversationDTO> data, CancellationToken cancellationToken = default);

        /// <summary>Xóa cache của 1 user - gọi ngay sau khi có hành động làm đổi danh sách/ thứ tự hội thoại của user đó (gửi tin mới...).</summary>
        Task InvalidateAsync(int viewerId, CancellationToken cancellationToken = default);
    }
}
