using Microsoft.Data.SqlClient;

namespace TrainingAPI.Services.Common
{
    /// <summary>
    /// [2.1][Composition] Class này "có chứa" (has-a) cả một SqlConnection
    /// lẫn một SqlDataReader, và chịu trách nhiệm giải phóng CẢ HAI theo
    /// đúng thứ tự khi caller dùng xong.
    ///
    /// Lý do cần class riêng thay vì trả thẳng SqlDataReader: reader chỉ
    /// đọc được khi connection đứng sau nó còn MỞ. Nếu ISqlDataAccess tự
    /// "using" và đóng connection ngay bên trong phương thức của nó rồi
    /// mới trả reader ra ngoài, reader sẽ chết ngay khi caller bắt đầu
    /// vòng lặp đọc. SqlReaderResult giữ cả hai sống tới khi caller chủ
    /// động "await using" xong.
    /// </summary>
    public sealed class SqlReaderResult : IAsyncDisposable
    {
        private readonly SqlConnection _connection;

        public SqlDataReader Reader { get; }

        internal SqlReaderResult(SqlConnection connection, SqlDataReader reader)
        {
            _connection = connection;
            Reader = reader;
        }

        public async ValueTask DisposeAsync()
        {
            // Reader được mở kèm CommandBehavior.CloseConnection nên việc
            // dispose reader thường đã tự đóng connection; dispose thêm
            // connection ở đây chỉ để chắc chắn tuyệt đối, không gây lỗi
            // nếu nó đã đóng sẵn.
            await Reader.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
