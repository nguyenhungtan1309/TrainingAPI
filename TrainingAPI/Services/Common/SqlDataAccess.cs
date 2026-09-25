using Microsoft.Data.SqlClient;
using System.Data;

namespace TrainingAPI.Services.Common
{
    public class SqlDataAccess : ISqlDataAccess
    {
        private readonly string _connectionString;

        public SqlDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Thiếu cấu hình ConnectionStrings:DefaultConnection.");
        }

        private static SqlCommand CreateCommand(
            SqlConnection connection,
            string commandText,
            CommandType commandType,
            Action<SqlParameterCollection>? configureParameters)
        {
            var command = new SqlCommand(commandText, connection) { CommandType = commandType };
            configureParameters?.Invoke(command.Parameters);
            return command;
        }

        public async Task<int> ExecuteNonQueryAsync(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = CreateCommand(connection, storedProcedure, CommandType.StoredProcedure, configureParameters);

            await connection.OpenAsync(cancellationToken);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<int> ExecuteRawNonQueryAsync(
            string sqlText,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = CreateCommand(connection, sqlText, CommandType.Text, configureParameters);

            await connection.OpenAsync(cancellationToken);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<T?> ExecuteScalarAsync<T>(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = CreateCommand(connection, storedProcedure, CommandType.StoredProcedure, configureParameters);

            await connection.OpenAsync(cancellationToken);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null || result is DBNull) return default;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public async Task<SqlReaderResult> ExecuteReaderAsync(
            string storedProcedure,
            Action<SqlParameterCollection>? configureParameters = null,
            CancellationToken cancellationToken = default)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                var command = CreateCommand(connection, storedProcedure, CommandType.StoredProcedure, configureParameters);

                await connection.OpenAsync(cancellationToken);

                var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken);
                return new SqlReaderResult(connection, reader);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }
}