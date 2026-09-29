using Microsoft.Data.SqlClient;

namespace TrainingAPI.Services.Common
{
    public interface ISqlDataAccess
    {
        Task<int> ExecuteNonQueryAsync(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default);

        Task<T?> ExecuteScalarAsync<T>(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default);

        Task<SqlReaderResult> ExecuteReaderAsync(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default);

        Task<int> ExecuteRawNonQueryAsync(
            string sqlText,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default);
    }
}
