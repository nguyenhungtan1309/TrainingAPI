using Microsoft.Data.SqlClient;

namespace TrainingAPI.Services.Common
{
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
            await Reader.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
