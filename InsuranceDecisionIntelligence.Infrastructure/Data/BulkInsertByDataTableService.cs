using InsuranceDecisionIntelligence.Application.Common.Models;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Data
{
    public class BulkInsertByDataTableService : IBulkInsertByDataTableService
    {
        private readonly ConnectionSettings _connectionString;
        private readonly ILogger<BulkInsertByIDataReaderService> _logger;

        public BulkInsertByDataTableService(IOptions<ConnectionSettings> options, ILogger<BulkInsertByIDataReaderService> logger)
        {
            _connectionString = options.Value;
            _logger = logger;
        }
        public async Task InsertAsync(string fileName, DataTable dataTable)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            NormalizeColumns(dataTable);

            using var transaction = conn.BeginTransaction();

            try
            {
                //Read type	  batch Size	bulk option	enable streaming	copy file	read data	Bulk Insert	 Insert finished
                //data table	 50000  	table lock  	F	            52	        6268        	33473	    34099

                string baseName = CleanName(Path.GetFileNameWithoutExtension(fileName));
                string tableName = $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}";

                var sw = Stopwatch.StartNew();

                await CreateTableAsync(tableName,dataTable,conn,transaction);
                await InsertDataAsync(tableName,dataTable,conn,transaction);
                await transaction.CommitAsync();

                sw.Stop();
                _logger.LogInformation("TOTAL INSERT: {ms}", sw.ElapsedMilliseconds);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ================= NORMALIZE =================
        private void NormalizeColumns(DataTable dt)
        {
            foreach (DataColumn col in dt.Columns)
            {
                col.ColumnName = CleanName(col.ColumnName);
            }
        }

        private string CleanName(string name)
        {
            var cleaned = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
            cleaned = Regex.Replace(cleaned, "_+", "_");
            cleaned = cleaned.Trim('_');

            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = "Col_" + Guid.NewGuid().ToString("N")[..6];

            return cleaned;
        }

        // ================= BULK INSERT (FAST) =================
        private async Task InsertDataAsync(
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
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(dataTable);
        }

        // ================= CREATE TABLE =================
        private async Task CreateTableAsync(
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
}
