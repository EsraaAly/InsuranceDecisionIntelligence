using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using InsuranceDecisionIntelligence.Application.Common.Models;
using InsuranceDecisionIntelligence.Application.DTOs.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Infrastructure.Data.Bulk;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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

        public DatabaseMetaDataService(IOptions<ConnectionSettings> options,ILogger<DatabaseMetaDataService> logger,IMemoryCache memoryCache)
        {
            _connectionString = options.Value;
            _logger = logger;
            _memoryCache = memoryCache;
        }
        //public async Task<string> GetTableName(string Path)
        //{
        //    if (!_memoryCache.TryGetValue(Path, out string tableName))
        //    {
        //        using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);

        //        string query = $"Select Top 1 [tableName] from [UploadLog] where [path] = @path order by id desc";

        //         tableName = await conn.QueryFirstOrDefaultAsync<string>(query, new { path = Path });
        //    }
        //    return tableName;
        //}

        public async Task<string> GetTableNameByPath(string Path)
        {
            if (!_memoryCache.TryGetValue(Path, out string tableName))
            {
                using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
                await conn.OpenAsync();

                string query = $"Select Top 1 [tableName] from [UploadLog] where IsActive = 1 and [path] = '{Path}' order by id desc";
                using var cmd = new SqlCommand(query, conn);
                var columnValue = await cmd.ExecuteScalarAsync();
                tableName = columnValue.ToString();
            }

            return tableName;

        }
        public async Task<string> GetTableNameById(int Id)
        {
            if (!_memoryCache.TryGetValue(Id, out string tableName))
            {
                using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
                await conn.OpenAsync();

                string query = $"Select [tableName] from [UploadLog] where IsActive = 1 and id = {Id}";
                using var cmd = new SqlCommand(query, conn);
                var columnValue = await cmd.ExecuteScalarAsync();
                tableName = columnValue.ToString();
                await conn.CloseAsync();
            }

            return tableName;

        }

        public async Task<IEnumerable<GetUploadedFilesDto>> GetUploadedFilesAsync()
        {

           
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);

            if (!await TableExistsAsync("UploadLog"))
            {
                return Enumerable.Empty<GetUploadedFilesDto>();
            }
            string query = $"Select top 1 Id,filename,UploadedAt from [UploadLog] where IsActive = 1 order by id desc";


            var result = await conn.QueryAsync<GetUploadedFilesDto>(query);

            return result;

        }

        public async Task<Dictionary<string,object>> GetTableDetailsByIdAsync(int Id)
        {

            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            string query = $"Select TableName, UploadedAt UploadedDate from [UploadLog] where IsActive = 1 and id = {Id}";
            using var cmd = new SqlCommand(query, conn);
            var reader = await cmd.ExecuteReaderAsync();
            var result = new Dictionary<string,object>();
            while (reader.Read())
            {

                result.Add("TableName", reader.GetString("TableName"));
                result.Add("UploadedDate", reader.GetDateTime("UploadedDate"));
            }

            string tablename = result["TableName"].ToString();
            int RowsCount = await GetRowCount(tablename);
            int ColumnsCount = await GetColumnsCount(tablename);

            result.Add("RowsCount", RowsCount);
            result.Add("ColumnsCount", ColumnsCount);
            await conn.CloseAsync();
            return result;

        }
        private async Task<int> GetRowCount(string tableName)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            string query = $"Select count(*) RowsCount from [{tableName}] ";
            using var cmd = new SqlCommand(query, conn);
            int RowsCount = (int) await cmd.ExecuteScalarAsync();
            await conn.CloseAsync();
            return RowsCount;

        }
        private async Task<int> GetColumnsCount(string tableName)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            string query = $"Select count(*) ColumnsCount from Information_Schema.COLUMNS where Table_Name= '{tableName}' ";
            using var cmd = new SqlCommand(query, conn);
            int ColumnsCount = (int)await cmd.ExecuteScalarAsync() -   1;
            return ColumnsCount;

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

    }
}
