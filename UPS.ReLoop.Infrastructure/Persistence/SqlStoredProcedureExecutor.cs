namespace UPS.ReLoop.Infrastructure.Persistence;

using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using UPS.ReLoop.Application.Interfaces;

/// <summary>
/// Reusable stored procedure executor using ADO.NET over the EF Core DbContext connection.
/// Supports typed queries, scalar results, non-query execution, transactions, and structured logging.
/// </summary>
public class SqlStoredProcedureExecutor : ISqlStoredProcedureExecutor
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SqlStoredProcedureExecutor> _logger;

    public SqlStoredProcedureExecutor(ApplicationDbContext context, ILogger<SqlStoredProcedureExecutor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<T>> ExecuteAsync<T>(
        string storedProcedureName,
        IEnumerable<IDbDataParameter>? parameters = null,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        _logger.LogInformation("Executing SP: {SpName}", storedProcedureName);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = CreateCommand(connection, storedProcedureName, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<T>();
        var properties = GetMappableProperties<T>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var item = MapRow<T>(reader, properties);
            results.Add(item);
        }

        _logger.LogInformation("SP {SpName} returned {Count} rows", storedProcedureName, results.Count);
        return results.AsReadOnly();
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string storedProcedureName,
        IEnumerable<IDbDataParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing scalar SP: {SpName}", storedProcedureName);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = CreateCommand(connection, storedProcedureName, parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result == DBNull.Value)
            return default;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<int> ExecuteNonQueryAsync(
        string storedProcedureName,
        IEnumerable<IDbDataParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing non-query SP: {SpName}", storedProcedureName);

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = CreateCommand(connection, storedProcedureName, parameters);

        // Use existing transaction if one is active
        var transaction = _context.Database.CurrentTransaction;
        if (transaction is not null)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("SP {SpName} affected {Rows} rows", storedProcedureName, rowsAffected);
        return rowsAffected;
    }

    private static System.Data.Common.DbCommand CreateCommand(
        System.Data.Common.DbConnection connection,
        string storedProcedureName,
        IEnumerable<IDbDataParameter>? parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = 30;

        if (parameters is not null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }
        }

        return command;
    }

    private static async Task EnsureConnectionOpenAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static PropertyInfo[] GetMappableProperties<T>() where T : class
    {
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();
    }

    private static T MapRow<T>(System.Data.Common.DbDataReader reader, PropertyInfo[] properties) where T : class, new()
    {
        var item = new T();
        var columns = Enumerable.Range(0, reader.FieldCount)
            .ToDictionary(i => reader.GetName(i), i => i, StringComparer.OrdinalIgnoreCase);

        foreach (var prop in properties)
        {
            if (!columns.TryGetValue(prop.Name, out var ordinal))
                continue;

            if (reader.IsDBNull(ordinal))
                continue;

            var value = reader.GetValue(ordinal);
            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            prop.SetValue(item, Convert.ChangeType(value, targetType));
        }

        return item;
    }
}
