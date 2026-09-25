using Microsoft.Data.SqlClient;

namespace TrainingAPI.Services.Common
{
    /// <summary>
    /// [2.1][Interface][Abstraction] Hợp đồng cho việc gọi stored
    /// procedure (và, khi thật sự cần, một câu SQL thuần).
    ///
    /// TRƯỚC KHI CÓ INTERFACE NÀY: gần như MỌI action trong
    /// ChatController, UserController, và cả ChatHub (SignalR) đều tự
    /// lặp lại nguyên văn đoạn code:
    ///     using var conn = new SqlConnection(_connectionString);
    ///     using var cmd = new SqlCommand("...", conn) { CommandType = ... };
    ///     cmd.Parameters.AddWithValue(...);
    ///     await conn.OpenAsync();
    ///     ...
    /// Đây chính là ví dụ thực tế của vấn đề mục 2.2 (Dependency
    /// Injection) sẽ phân tích kỹ ở đợt sau: mỗi class tự "new" lấy
    /// phụ thuộc (ở đây là kết nối DB) thay vì nhận nó từ bên ngoài.
    /// ISqlDataAccess gom toàn bộ việc mở/đóng connection, tạo command,
    /// gán CommandType vào MỘT nơi duy nhất; Controller/Hub giờ chỉ còn
    /// việc khai báo "gọi SP nào, với tham số gì".
    /// </summary>
    public interface ISqlDataAccess
    {
        /// <summary>Gọi một stored procedure không cần đọc dữ liệu trả về (INSERT/UPDATE/DELETE thuần).</summary>
        Task<int> ExecuteNonQueryAsync(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>Gọi một stored procedure và lấy giá trị đơn ở cột đầu, dòng đầu của kết quả (ví dụ Id vừa tạo).</summary>
        Task<T?> ExecuteScalarAsync<T>(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>Gọi một stored procedure trả về (một hoặc nhiều) result set để đọc bằng SqlDataReader.</summary>
        Task<SqlReaderResult> ExecuteReaderAsync(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Chạy một câu lệnh SQL THUẦN (không phải stored procedure).
        /// Chỉ dùng cho những chỗ hiếm hoi chưa có SP tương ứng
        /// (ví dụ SaveAttachment hiện tại) - KHÔNG dùng để ghép chuỗi từ
        /// input người dùng (xem nguyên tắc Dynamic SQL đã phân tích ở
        /// phần SQL nâng cao mục 1.3: giá trị luôn qua tham số hóa).
        /// </summary>
        Task<int> ExecuteRawNonQueryAsync(
            string sqlText,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default);
    }
}
