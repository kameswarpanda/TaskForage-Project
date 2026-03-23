using System.Data;

namespace TaskForge.Domain.Interfaces;

/// <summary>
/// Interface for executing stored procedures via ADO.NET.
/// </summary>
public interface IStoredProcedureExecutor
{
    Task<IEnumerable<T>> ExecuteQueryAsync<T>(string procedureName, object? parameters = null) where T : class, new();
    Task<T?> ExecuteScalarAsync<T>(string procedureName, object? parameters = null);
    Task<int> ExecuteNonQueryAsync(string procedureName, object? parameters = null);
    Task<DataSet> ExecuteDataSetAsync(string procedureName, object? parameters = null);
}
