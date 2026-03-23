using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Data;

/// <summary>
/// ADO.NET-based stored procedure executor for raw SQL access.
/// Demonstrates connection pooling via SqlConnection and manual parameter mapping.
/// </summary>
public class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly string _connectionString;

    public StoredProcedureExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    /// <summary>
    /// Executes a stored procedure and maps the result to a list of T.
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string procedureName, object? parameters = null) where T : class, new()
    {
        var results = new List<T>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(connection, procedureName, parameters);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        while (await reader.ReadAsync())
        {
            var item = new T();
            foreach (var prop in properties)
            {
                if (HasColumn(reader, prop.Name))
                {
                    var value = reader[prop.Name];
                    if (value != DBNull.Value)
                    {
                        prop.SetValue(item, Convert.ChangeType(value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
                    }
                }
            }
            results.Add(item);
        }

        return results;
    }

    /// <summary>
    /// Executes a stored procedure and returns a scalar value.
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(string procedureName, object? parameters = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(connection, procedureName, parameters);

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();

        if (result == null || result == DBNull.Value)
            return default;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    /// <summary>
    /// Executes a stored procedure that doesn't return data (INSERT, UPDATE, DELETE).
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string procedureName, object? parameters = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(connection, procedureName, parameters);

        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Executes a stored procedure returning multiple result sets via DataSet.
    /// </summary>
    public async Task<DataSet> ExecuteDataSetAsync(string procedureName, object? parameters = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(connection, procedureName, parameters);

        var dataSet = new DataSet();
        using var adapter = new SqlDataAdapter(command);

        await connection.OpenAsync();
        adapter.Fill(dataSet);

        return dataSet;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string procedureName, object? parameters)
    {
        var command = new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        if (parameters != null)
        {
            foreach (var prop in parameters.GetType().GetProperties())
            {
                var value = prop.GetValue(parameters) ?? DBNull.Value;
                command.Parameters.AddWithValue($"@{prop.Name}", value);
            }
        }

        return command;
    }

    private static bool HasColumn(IDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
