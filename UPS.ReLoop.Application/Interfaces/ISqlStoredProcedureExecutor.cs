namespace UPS.ReLoop.Application.Interfaces;

using System.Data;

/// <summary>
/// Reusable stored procedure executor service.
/// Provides typed query execution, scalar execution, and non-query execution
/// with transaction support, logging, and async execution.
/// </summary>
public interface ISqlStoredProcedureExecutor
{
    /// <summary>
    /// Executes a stored procedure and returns a list of strongly typed results.
    /// </summary>
    Task<IReadOnlyList<T>> ExecuteAsync<T>(
        string storedProcedureName,
        IEnumerable<IDbDataParameter>? parameters = null,
        CancellationToken cancellationToken = default) where T : class, new();

    /// <summary>
    /// Executes a stored procedure and returns a single scalar value.
    /// </summary>
    Task<T?> ExecuteScalarAsync<T>(
        string storedProcedureName,
        IEnumerable<IDbDataParameter>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure that does not return a result set.
    /// Returns the number of rows affected.
    /// </summary>
    Task<int> ExecuteNonQueryAsync(
        string storedProcedureName,
        IEnumerable<IDbDataParameter>? parameters = null,
        CancellationToken cancellationToken = default);
}
