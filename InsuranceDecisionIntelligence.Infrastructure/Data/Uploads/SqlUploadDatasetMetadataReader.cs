using Dapper;
using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Configuration;
using InsuranceDecisionIntelligence.Application.DTOs.Uploads;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InsuranceDecisionIntelligence.Infrastructure.Data.Uploads;

public class SqlUploadDatasetMetadataReader : IUploadDatasetMetadataReader
{
    private readonly DatabaseConnectionOptions _connectionOptions;
    private readonly ILogger<SqlUploadDatasetMetadataReader> _logger;
    private readonly IMemoryCache _memoryCache;

    public SqlUploadDatasetMetadataReader(
        IOptions<DatabaseConnectionOptions> options,
        ILogger<SqlUploadDatasetMetadataReader> logger,
        IMemoryCache memoryCache)
    {
        _connectionOptions = options.Value;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<string?> GetTableNameByFilePathAsync(string filePath)
    {
        if (!_memoryCache.TryGetValue(filePath, out string? tableName))
        {
            using SqlConnection conn = new(_connectionOptions.DefaultConnection);
            await conn.OpenAsync();

            const string query = "SELECT TOP 1 [tableName] FROM [UploadLog] WHERE IsActive = 1 AND [path] = @path ORDER BY id DESC";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@path", filePath);
            var columnValue = await cmd.ExecuteScalarAsync();
            tableName = columnValue?.ToString();
        }

        return tableName;
    }

    public async Task<string?> GetTableNameByUploadIdAsync(int uploadId)
    {
        if (_memoryCache.TryGetValue(uploadId, out string? cached))
            return cached;

        using var conn = new SqlConnection(_connectionOptions.DefaultConnection);

        const string query = "SELECT [tableName] FROM [UploadLog] WHERE IsActive = 1 AND Id = @Id";

        var tableName = await conn.ExecuteScalarAsync<string>(query, new { Id = uploadId });

        if (!string.IsNullOrEmpty(tableName))
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromDays(1));

            _memoryCache.Set(uploadId, tableName, cacheOptions);
        }

        return tableName;
    }

    public async Task<IEnumerable<UploadedFileSummaryDto>> GetUploadedFileSummariesAsync()
    {
        bool isExists = await TableExistsAsync("UploadLog");
        if (!isExists)
        {
            _logger.LogWarning("UploadLog table does not exist.");
            return Enumerable.Empty<UploadedFileSummaryDto>();
        }

        using SqlConnection conn = new(_connectionOptions.DefaultConnection);
        await conn.OpenAsync();

        const string query = @"
                SELECT
                    u.Id,
                    u.FileName,
                    CONVERT(NVARCHAR(30), u.UploadedAt, 120) AS UploadedAt
                FROM UploadLog u
                INNER JOIN
                (
                    SELECT
                        FileName,
                        MAX(Id) AS MaxId
                    FROM UploadLog
                    WHERE IsActive = 1
                    GROUP BY FileName
                ) x
                ON u.FileName = x.FileName
                AND u.Id = x.MaxId
                ORDER BY u.Id DESC";

        return await conn.QueryAsync<UploadedFileSummaryDto>(query);
    }

    public async Task<Dictionary<string, object>> GetTableDetailsByUploadIdAsync(int id)
    {
        using SqlConnection conn = new(_connectionOptions.DefaultConnection);
        await conn.OpenAsync();

        const string query = "SELECT TableName, UploadedAt AS UploadedDate FROM [UploadLog] WHERE IsActive = 1 AND id = @Id";
        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        var reader = await cmd.ExecuteReaderAsync();

        var result = new Dictionary<string, object>();
        while (await reader.ReadAsync())
        {
            result.Add("TableName", reader.GetString(reader.GetOrdinal("TableName")));
            result.Add("UploadedDate", reader.GetDateTime(reader.GetOrdinal("UploadedDate")));
        }

        string tablename = result["TableName"].ToString()!;
        long rowsCount = await GetRowCount(tablename);
        int columnsCount = await GetColumnsCount(tablename);

        result.Add("RowsCount", rowsCount);
        result.Add("ColumnsCount", columnsCount);
        await conn.CloseAsync();
        return result;
    }

    private async Task<long> GetRowCount(string tableName)
    {
        using SqlConnection conn = new(_connectionOptions.DefaultConnection);
        await conn.OpenAsync();

        const string query = @"
                SELECT SUM(row_count)
                FROM sys.dm_db_partition_stats
                WHERE object_id = OBJECT_ID(@TableName)
                AND index_id IN (0,1)";

        using SqlCommand cmd = new(query, conn);
        cmd.Parameters.AddWithValue("@TableName", tableName);

        object? scalar = await cmd.ExecuteScalarAsync();

        return scalar != DBNull.Value && scalar != null
            ? Convert.ToInt64(scalar)
            : 0;
    }

    private async Task<int> GetColumnsCount(string tableName)
    {
        using SqlConnection conn = new(_connectionOptions.DefaultConnection);
        await conn.OpenAsync();

        const string query = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                AND COLUMN_NAME <> 'TableId'";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@TableName", tableName);

        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<bool> TableExistsAsync(string tableName)
    {
        using SqlConnection conn = new(_connectionOptions.DefaultConnection);
        await conn.OpenAsync();

        const string sql = @"
                SELECT 1 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @tableName 
                AND TABLE_SCHEMA = 'dbo'";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tableName", tableName);

        var result = await cmd.ExecuteScalarAsync();
        await conn.CloseAsync();
        return result != null;
    }

    public async Task<IEnumerable<dynamic>> GetDynamicChartData(
        string tableName,
        string xAxisColumn,
        string yAxisColumn,
        string aggregationType,
        bool top10Only)
    {
        string limit = top10Only ? "TOP 10" : "";
        using SqlConnection conn = new(_connectionOptions.DefaultConnection);

        string sql = $@"
                SELECT {limit} 
                    [{xAxisColumn}] AS Label, 
                    {aggregationType}(TRY_CAST(NULLIF([{yAxisColumn}], '') AS FLOAT)) AS Value
                FROM [{tableName}] WITH (NOLOCK)
                WHERE [{xAxisColumn}] IS NOT NULL
                GROUP BY [{xAxisColumn}]
                ORDER BY Value DESC
                OPTION (MAXDOP 4)";

        return await conn.QueryAsync(sql);
    }
}
