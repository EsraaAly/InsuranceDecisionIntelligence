using Dapper;
using InsuranceDecisionIntelligence.Application.Common.Models;
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

        public async Task<string> GetTableName(string Path)
        {
            if (!_memoryCache.TryGetValue(Path, out string tableName))
            {
                using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
                await conn.OpenAsync();

                string query = $"Select Top 1 [tableName] from [UploadLog] where [path] = '{Path}' order by id desc";
                using var cmd = new SqlCommand(query, conn);
                var columnValue = await cmd.ExecuteScalarAsync();
                tableName = columnValue.ToString();
            }

            return tableName;

        }
    }
}
