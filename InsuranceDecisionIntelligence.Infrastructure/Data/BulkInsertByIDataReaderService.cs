using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using InsuranceDecisionIntelligence.Application.Common.Models;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Application.Services.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Data
{
    public class BulkInsertByIDataReaderService : IBulkInsertByIDataReaderService
    {
        private readonly ConnectionSettings _connectionString;
        private readonly ILogger<BulkInsertByIDataReaderService> _logger;

        public BulkInsertByIDataReaderService(IOptions<ConnectionSettings> options, ILogger<BulkInsertByIDataReaderService> logger)
        {
            _connectionString = options.Value;
            _logger = logger;
        }

        // ================= MAIN =================


        public async Task InsertAsync(string path,string fileName, IDataReader reader)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            string baseName = CleanName(Path.GetFileNameWithoutExtension(fileName));
            string tableName = $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}";

            // 1. get columns

            var columns = new List<string>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i);

                if (string.IsNullOrWhiteSpace(name))
                    name = $"Column{i}";

                columns.Add(CleanName(name));
            }

            using (var transaction = conn.BeginTransaction())
            {

                try
                {

                    // 2. create table
                    //var sw = Stopwatch.StartNew();

                    //await CreateTableAsync(tableName, columns, conn, transaction);

                    //_logger.LogInformation("CreateTable: {ms}", sw.ElapsedMilliseconds);

                    //sw.Restart();
                    //await InsertDataAsync(tableName, reader, columns, conn, transaction);

                    //_logger.LogInformation(message: "BulkInsert: {ms}", sw.ElapsedMilliseconds);

                    //await transaction.CommitAsync();

                    var sw = Stopwatch.StartNew();

                    await CreateTableAsync(tableName, columns, conn, transaction);
                    await transaction.CommitAsync();
                    await InsertDataAsync(tableName, reader, columns, conn, null);
                    //await transaction.CommitAsync();
                    sw.Stop();
                    _logger.LogInformation("Bulk Insert: {ms}", sw.ElapsedMilliseconds);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    await conn.CloseAsync();
                    throw;
                }
            }

            //var swConvert = Stopwatch.StartNew();

            //await ConvertTableTypesAsync(tableName, columns, conn, null);
            //swConvert.Stop();
            //_logger.LogInformation("convert Table: {ms}", swConvert.ElapsedMilliseconds);

            bool isExist = await TableExistsAsync(conn, "UploadLog");
            if (!isExist)
            {
                await CreateLogTableAsync(conn, null);
            }
            await InsertLogDataAsync(tableName, fileName, path, conn,null);
            
        }


        private async Task CreateTableAsync(
                         string tableName,
                         List<string> columns,
                         SqlConnection conn,
                         SqlTransaction transaction)
        {

           var sb = new StringBuilder();

           for (int i = 0; i < columns.Count; i++)
           {
               if (i > 0) sb.Append(",");

               string original = columns[i];
               string col = original.ToLower();

               //// DATE
               //if (col.Contains("date"))
               //{
               //    sb.Append($"[{original}] DATETIME NULL");
               //}
               //// INT (more strict)
               //else if (col.EndsWith("id") ||
               //         col.Contains("_id") ||
               //         col.Contains("year") ||
               //         col.Contains("count") ||
               //         col.Contains("qty") ||
               //         col.StartsWith("n_"))
               //{
               //    sb.Append($"[{original}] INT");
               //}
               //// DECIMAL (financial only)
               //else if (col.Contains("premium") ||
               //         col.Contains("price") ||
               //         col.Contains("cost") ||
               //         col.Contains("amount"))
               //{
               //    sb.Append($"[{original}] DECIMAL(18,2)");
               //}
               //else
               //{
               //    sb.Append($"[{original}] NVARCHAR(500)");
               //}
               sb.Append($"[{original}] NVARCHAR(500)");
           }

            string sql = $@"
                        CREATE TABLE [{tableName}] (
                        {sb}
                        )";

                
            
            using var cmd = new SqlCommand(sql, conn, transaction);
            await cmd.ExecuteNonQueryAsync();
        }



        private async Task InsertDataAsync(
                        string tableName,
                        IDataReader reader,
                        List<string> columns,
                        SqlConnection conn,
                        SqlTransaction transaction)
        {
            using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, null)
            {
                DestinationTableName = tableName,
                BatchSize = 50000,
                BulkCopyTimeout = 0,
                EnableStreaming = true
            };

            var swmapping = Stopwatch.StartNew();

            for (int i = 0; i < columns.Count; i++)
            {
                bulkCopy.ColumnMappings.Add(i, columns[i]);
            }
            swmapping.Stop();
            _logger.LogInformation("Mapping Coulmns: {ms}", swmapping.ElapsedMilliseconds);
            //bulkCopy.NotifyAfter = 10000;

            //bulkCopy.SqlRowsCopied += (s, e) =>
            //{
            //    _logger.LogInformation("Copied: {rows}", e.RowsCopied);
            //};
            var swwritetoserves = Stopwatch.StartNew();

            await bulkCopy.WriteToServerAsync(reader);
            swwritetoserves.Stop();
            _logger.LogInformation("Write to server: {ms}", swwritetoserves.ElapsedMilliseconds);
        }

        private async Task ConvertTableTypesAsync(
                                string rawTableName,
                                List<string> columns,
                                SqlConnection conn,
                                SqlTransaction transaction)
        {

            string finalTable = rawTableName + "_Final";

            var create = new StringBuilder();

            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    create.Append(",");

                string col = columns[i];

                string type = await DetectColumnTypeAsync(
                    rawTableName,
                    col,
                    conn,
                    transaction);

                create.Append($"[{col}] {type} NULL");
            }

            // create final table
            string createSql = $@"
                                CREATE TABLE [{finalTable}]
                                (
                                    {create}
                                )";

            using (var cmd = new SqlCommand(createSql, conn, transaction))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // insert converted data
            var insert = new StringBuilder();

            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                    insert.Append(",");

                string col = columns[i];

                string type = await DetectColumnTypeAsync(
                    rawTableName,
                    col,
                    conn,
                    transaction);

                if (type == "INT")
                {
                    insert.Append($"TRY_CAST([{col}] AS INT)");
                }
                else if (type == "DECIMAL(18,2)")
                {
                    insert.Append($"TRY_CAST([{col}] AS DECIMAL(18,2))");
                }
                else if (type == "DATETIME")
                {
                    insert.Append($"TRY_CAST([{col}] AS DATETIME)");
                }
                else
                {
                    insert.Append($"[{col}]");
                }
            }

            string insertSql = $@"
                                INSERT INTO [{finalTable}]
                                SELECT
                                    {insert}
                                FROM [{rawTableName}]";

            using (var cmd = new SqlCommand(insertSql, conn, transaction))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<string> DetectColumnTypeAsync(
                                    string tableName,
                                    string columnName,
                                    SqlConnection conn,
                                    SqlTransaction transaction)
        {
            string sql = $@"
                        SELECT TOP 100 [{columnName}]
                        FROM [{tableName}]
                        WHERE [{columnName}] IS NOT NULL 
                        AND LTRIM(RTRIM([{columnName}])) <> ''";

            var values = new List<string>();

            using (var cmd = new SqlCommand(sql, conn, transaction))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    values.Add(reader[0].ToString());
                }
            }

            if (values.Count == 0)
                return "NVARCHAR(500)";

            bool allInt = true;
            bool allDecimal = true;
            bool allDate = true;
            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd","MM/dd/yyyy" };

            foreach (var val in values)
            {
                if (!int.TryParse(val, out _))
                    allInt = false;

                if (!decimal.TryParse(val, out _))
                    allDecimal = false;

                if (!DateTime.TryParseExact(val,formats,CultureInfo.InvariantCulture,DateTimeStyles.None, out _))
                    allDate = false;
            }

            if (allInt)
                return "INT";

            if (allDecimal)
                return "DECIMAL(18,2)";

            if (allDate)
                return "DATETIME";

            return "NVARCHAR(500)";
        }

        private async Task CreateLogTableAsync(
                         SqlConnection conn,
                         SqlTransaction transaction)
        {
            var sw = Stopwatch.StartNew();

            string sql = "CREATE TABLE UploadLog (Id int Identity(1,1),path NVARCHAR(250),fileName NVARCHAR(250),tableName NVARCHAR(250),)";
            using var cmd = new SqlCommand(sql, conn,transaction);
            await cmd.ExecuteNonQueryAsync();

            sw.Stop();
            _logger.LogInformation("Create Log Table: {ms}", sw.ElapsedMilliseconds);
        }
        private async Task InsertLogDataAsync(
                         string tableName,
                         string filename,
                         string path,
                         SqlConnection conn,
                         SqlTransaction transaction)
        {
            var sw = Stopwatch.StartNew();

            
            string sql = $"Insert into UploadLog (path,filename,tablename) values('{path}','{filename}','{tableName}')";
            using var cmd = new SqlCommand(sql, conn,transaction);
            await cmd.ExecuteNonQueryAsync();

            sw.Stop();
            _logger.LogInformation("insert Log Data: {ms}", sw.ElapsedMilliseconds);
        }

        // ================= CLEAN NAME =================
        private string CleanName(string name)
        {
            var cleaned = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
            cleaned = Regex.Replace(cleaned, "_+", "_");
            cleaned = cleaned.Trim('_');

            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = "Col_" + Guid.NewGuid().ToString("N")[..6];

            return cleaned;
        }

        // ================= TABLE EXISTS =================
        private async Task<bool> TableExistsAsync(SqlConnection conn, string tableName)
        {
            var sql = @"
                        SELECT 1 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME = @tableName 
                        AND TABLE_SCHEMA = 'dbo'";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tableName", tableName);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }
    }
}
