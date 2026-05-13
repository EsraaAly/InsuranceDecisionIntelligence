using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace InsuranceDecisionIntelligence.Infrastructure.Data.Import;

public class DataTableSqlBulkImporter : IDataTableSqlBulkImporter
{
    private readonly DatabaseConnectionOptions _connectionOptions;
    private readonly ILogger<DataTableSqlBulkImporter> _logger;

    public DataTableSqlBulkImporter(IOptions<DatabaseConnectionOptions> options, ILogger<DataTableSqlBulkImporter> logger)
    {
        _connectionOptions = options.Value;
        _logger = logger;
    }

    public async Task ImportAsync(string fileName, DataTable dataTable)
    {
        using SqlConnection conn = new(_connectionOptions.DefaultConnection);
        await conn.OpenAsync();

        NormalizeColumns(dataTable);

        using var transaction = conn.BeginTransaction();

        try
        {
            string baseName = CleanName(Path.GetFileNameWithoutExtension(fileName));
            string tableName = $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}";

            var stopwatch = Stopwatch.StartNew();

            await CreateTableAsync(tableName, dataTable, conn, transaction);
            await InsertDataAsync(tableName, dataTable, conn, transaction);
            await transaction.CommitAsync();

            stopwatch.Stop();
            _logger.LogInformation("SqlBulk import completed for table '{TableName}' with {RowCount} rows and {ColumnCount} columns in {ElapsedMilliseconds} ms", tableName, dataTable.Rows.Count, dataTable.Columns.Count, stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static void NormalizeColumns(DataTable dt)
    {
        foreach (DataColumn col in dt.Columns)
            col.ColumnName = CleanName(col.ColumnName);
    }

    private static string CleanName(string name)
    {
        var cleaned = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
        cleaned = Regex.Replace(cleaned, "_+", "_");
        cleaned = cleaned.Trim('_');

        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "Col_" + Guid.NewGuid().ToString("N")[..6];

        return cleaned;
    }

    private static async Task InsertDataAsync(
        string tableName,
        DataTable dataTable,
        SqlConnection conn,
        SqlTransaction transaction)
    {
        using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, transaction)
        {
            DestinationTableName = tableName,
            BatchSize = 50000,
            BulkCopyTimeout = 0,
            EnableStreaming = false
        };

        foreach (DataColumn column in dataTable.Columns)
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        await bulkCopy.WriteToServerAsync(dataTable);
    }

    private static async Task CreateTableAsync(
        string tableName,
        DataTable dataTable,
        SqlConnection conn,
        SqlTransaction transaction)
    {
        var sb = new StringBuilder();
        bool first = true;

        foreach (DataColumn column in dataTable.Columns)
        {
            if (!first)
                sb.Append(",");

            sb.Append($"[{column.ColumnName}] NVARCHAR(MAX)");
            first = false;
        }

        var sql = $@"
                        CREATE TABLE [{tableName}] (
                        {sb}
                        )";

        using var cmd = new SqlCommand(sql, conn, transaction);
        await cmd.ExecuteNonQueryAsync();
    }
}
