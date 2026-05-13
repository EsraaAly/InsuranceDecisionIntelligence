using Dapper;
using InsuranceDecisionIntelligence.Application.Common.Models;
using InsuranceDecisionIntelligence.Application.DTOs.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Infrastructure.Data.Bulk;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Data
{
    public class DatabaseMetaDataService : IDatabaseMetaDataService
    {
        private readonly ConnectionSettings _connectionString;
        private readonly ILogger<DatabaseMetaDataService> _logger;
        private readonly IMemoryCache _memoryCache;

        public DatabaseMetaDataService(IOptions<ConnectionSettings> options, ILogger<DatabaseMetaDataService> logger, IMemoryCache memoryCache)
        {
            _connectionString = options.Value;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<string> GetTableNameByPathAsync(string path)
        {
            if (!_memoryCache.TryGetValue(path, out string tableName))
            {
                using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
                await conn.OpenAsync();

                string query = "SELECT TOP 1 [tableName] FROM [UploadLog] WHERE IsActive = 1 AND [path] = @path ORDER BY id DESC";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@path", path);
                var columnValue = await cmd.ExecuteScalarAsync();
                tableName = columnValue?.ToString();
            }

            return tableName;
        }
        public async Task<string> GetTableNameByIdAsync(int id)
        {
            if (_memoryCache.TryGetValue(id, out string tableName))
            {
                return tableName;
            }

            using var conn = new SqlConnection(_connectionString.DefaultConnection);

            string query = "SELECT [tableName] FROM [UploadLog] WHERE IsActive = 1 AND Id = @Id";

            tableName = await conn.ExecuteScalarAsync<string>(query, new { Id = id });

            if (!string.IsNullOrEmpty(tableName))
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1)) 
                    .SetAbsoluteExpiration(TimeSpan.FromDays(1)); 

                _memoryCache.Set(id, tableName, cacheOptions);
            }

            return tableName;
        }

        public async Task<IEnumerable<GetUploadedFilesDto>> GetUploadedFilesAsync()
        {
            bool isExists = await TableExistsAsync("UploadLog");
            if (!isExists)
            {
                _logger.LogWarning("UploadLog table does not exist.");
                return Enumerable.Empty<GetUploadedFilesDto>();
            }

            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            const string query = @"
                SELECT
                    u.Id,
                    u.FileName,
                    u.UploadedAt
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

            return await conn.QueryAsync<GetUploadedFilesDto>(query);
        }

        public async Task<Dictionary<string, object>> GetTableDetailsByIdAsync(int id)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            string query = "SELECT TableName, UploadedAt AS UploadedDate FROM [UploadLog] WHERE IsActive = 1 AND id = @Id";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            var reader = await cmd.ExecuteReaderAsync();
            
            var result = new Dictionary<string, object>();
            while (reader.Read())
            {
                result.Add("TableName", reader.GetString("TableName"));
                result.Add("UploadedDate", reader.GetDateTime("UploadedDate"));
            }

            string tablename = result["TableName"].ToString();
            long RowsCount = await GetRowCount(tablename);
            int ColumnsCount = await GetColumnsCount(tablename);

            result.Add("RowsCount", RowsCount);
            result.Add("ColumnsCount", ColumnsCount);
            await conn.CloseAsync();
            return result;
        }
        private async Task<long> GetRowCount(string tableName)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            string query = @"
                SELECT SUM(row_count)
                FROM sys.dm_db_partition_stats
                WHERE object_id = OBJECT_ID(@TableName)
                AND index_id IN (0,1)";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TableName", tableName);

            object result = await cmd.ExecuteScalarAsync();

            return result != DBNull.Value
                ? Convert.ToInt64(result)
                : 0;
        }
        private async Task<int> GetColumnsCount(string tableName)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            const string query = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                AND COLUMN_NAME <> 'TableId'";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TableName", tableName);

            int columnsCount = (int)await cmd.ExecuteScalarAsync();
            return columnsCount;
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            var sql = @"
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
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            
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
}
