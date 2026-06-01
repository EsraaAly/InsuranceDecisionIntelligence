using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Data.Import;

public class DataReaderSqlBulkImporter : IDataReaderSqlBulkImporter
{
    private readonly DatabaseConnectionOptions _connectionOptions;
    private readonly ILogger<DataReaderSqlBulkImporter> _logger;
    private readonly IUploadDatasetMetadataReader _uploadMetadataReader;

    public DataReaderSqlBulkImporter(
        IOptions<DatabaseConnectionOptions> options,
        ILogger<DataReaderSqlBulkImporter> logger,
        IUploadDatasetMetadataReader uploadMetadataReader)
    {
        _connectionOptions = options.Value;
        _logger = logger;
        _uploadMetadataReader = uploadMetadataReader;
    }

    public async Task ImportAsync(string path, string fileName, IDataReader reader)
    {
        using SqlConnection conn = new(_connectionOptions.DefaultConnection);
            await conn.OpenAsync();

            string baseName = CleanName(Path.GetFileNameWithoutExtension(fileName));
            string tableName = $"{baseName}_{DateTime.Now:yyyyMMddHHmmssfff}";

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

                    await CreateTableAsync(tableName, columns, conn, transaction);
                    await transaction.CommitAsync();
                    var stopwatch = Stopwatch.StartNew();

                    await InsertDataAsync(tableName, reader, columns, conn, null);
                    stopwatch.Stop();
                    _logger.LogInformation("SqlBulk import completed for table '{TableName}' with {ColumnCount} columns in {ElapsedMilliseconds} ms", tableName, columns.Count, stopwatch.ElapsedMilliseconds);
                    var stopwatch_AddIndex = Stopwatch.StartNew();

                    //await AddIndexAsync(tableName, conn, null);
                    //stopwatch_AddIndex.Stop();
                    //_logger.LogInformation("AddIndex eted for table '{TableName}' in {ElapsedMilliseconds} ms", tableName, stopwatch_AddIndex.ElapsedMilliseconds);

            }
            catch
                {
                    await transaction.RollbackAsync();
                    await conn.CloseAsync();
                    throw;
                }
            }

            bool isExist = await _uploadMetadataReader.TableExistsAsync("UploadLog");
            if (!isExist)
            {
                await CreateLogTableAsync(conn, null);
                await CreateUploadLogIndexAsync(conn, null);
            }

            await UpdateLogForOldDataAsync(fileName, conn, null);
            await InsertLogDataAsync(tableName, fileName, path, conn, null);
        }


        private async Task CreateTableAsync(
            string tableName,
            List<string> columns,
            SqlConnection conn,
            SqlTransaction transaction)
        {
            var sb = new StringBuilder();
            sb.Append($"[TableId] int Identity(1,1) PRIMARY KEY "); 
            
            for (int i = 0; i < columns.Count; i++)
            {
                sb.Append(",");
                string original = columns[i];

                sb.Append($"[{original}] NVARCHAR(500)");
            }

            string sql = $@"
                        CREATE TABLE [{tableName}] (
                        {sb}
                        )";

            using var cmd = new SqlCommand(sql, conn, transaction);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddIndexAsync(
                string tableName,
                SqlConnection conn,
                SqlTransaction transaction)
        {
            string sql = $@"ALTER TABLE [{tableName}] ADD CONSTRAINT PK_{tableName} PRIMARY KEY CLUSTERED ([TableId]);";

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
            using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock |
                                                SqlBulkCopyOptions.UseInternalTransaction, transaction)
            {
                DestinationTableName = tableName,
                BatchSize = 50000,
                BulkCopyTimeout = 0,
                EnableStreaming = true
            };

            for (int i = 0; i < columns.Count; i++)
            {
                bulkCopy.ColumnMappings.Add(i, columns[i]);
            }

            await bulkCopy.WriteToServerAsync(reader);
        }

        //private async Task ConvertTableTypesAsync(
        //                        string rawTableName,
        //                        List<string> columns,
        //                        SqlConnection conn,
        //                        SqlTransaction transaction)
        //{

        //    string finalTable = rawTableName + "_Final";

        //    var create = new StringBuilder();

        //    for (int i = 0; i < columns.Count; i++)
        //    {
        //        if (i > 0)
        //            create.Append(",");

        //        string col = columns[i];

        //        string type = await DetectColumnTypeAsync(
        //            rawTableName,
        //            col,
        //            conn,
        //            transaction);

        //        create.Append($"[{col}] {type} NULL");
        //    }

        //    // create final table
        //    string createSql = $@"
        //                        CREATE TABLE [{finalTable}]
        //                        (
        //                            {create}
        //                        )";

        //    using (var cmd = new SqlCommand(createSql, conn, transaction))
        //    {
        //        await cmd.ExecuteNonQueryAsync();
        //    }

        //    // insert converted data
        //    var insert = new StringBuilder();

        //    for (int i = 0; i < columns.Count; i++)
        //    {
        //        if (i > 0)
        //            insert.Append(",");

        //        string col = columns[i];

        //        string type = await DetectColumnTypeAsync(
        //            rawTableName,
        //            col,
        //            conn,
        //            transaction);

        //        if (type == "INT")
        //        {
        //            insert.Append($"TRY_CAST([{col}] AS INT)");
        //        }
        //        else if (type == "DECIMAL(18,2)")
        //        {
        //            insert.Append($"TRY_CAST([{col}] AS DECIMAL(18,2))");
        //        }
        //        else if (type == "DATETIME")
        //        {
        //            insert.Append($"TRY_CAST([{col}] AS DATETIME)");
        //        }
        //        else
        //        {
        //            insert.Append($"[{col}]");
        //        }
        //    }

        //    string insertSql = $@"
        //                        INSERT INTO [{finalTable}]
        //                        SELECT
        //                            {insert}
        //                        FROM [{rawTableName}]";

        //    using (var cmd = new SqlCommand(insertSql, conn, transaction))
        //    {
        //        await cmd.ExecuteNonQueryAsync();
        //    }
        //}

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

        private async Task CreateLogTableAsync(SqlConnection conn,SqlTransaction transaction)
        {
            var sw = Stopwatch.StartNew();

            const string sql = @"
                            IF OBJECT_ID('UploadLog', 'U') IS NULL
                            BEGIN
                                CREATE TABLE UploadLog
                                (
                                    Id INT IDENTITY(1,1) PRIMARY KEY,
                                    FileName NVARCHAR(250),
                                    Path NVARCHAR(250),
                                    TableName NVARCHAR(250),
                                    UploadedAt DATETIME NOT NULL DEFAULT GETDATE(),
                                    IsActive BIT NOT NULL DEFAULT 1
                                )
                            END";

            using var cmd = new SqlCommand(sql, conn, transaction);
            await cmd.ExecuteNonQueryAsync();

            sw.Stop();
            _logger.LogInformation("UploadLog table created successfully in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
        }

        private async Task CreateUploadLogIndexAsync(SqlConnection conn, SqlTransaction transaction = null)
        {
            const string sql = @"
                                IF NOT EXISTS (
                                    SELECT name 
                                    FROM sys.indexes 
                                    WHERE name = 'IX_UploadLog_File_IsActive_Id'
                                      AND object_id = OBJECT_ID('UploadLog')
                                )
                                BEGIN
                                    CREATE INDEX IX_UploadLog_File_IsActive_Id
                                    ON UploadLog(FileName, IsActive, Id DESC)
                                END";

            using var cmd = new SqlCommand(sql, conn, transaction);

            cmd.CommandTimeout = 120; // index creation ممكن تاخد وقت
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task UpdateLogForOldDataAsync(
            string filename,
            SqlConnection conn,
            SqlTransaction transaction)
        {
            var stopwatch = Stopwatch.StartNew();

            string sql = $"UPDATE UploadLog SET IsActive = 0 WHERE FileName = @filename";
            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@filename", filename);
            await cmd.ExecuteNonQueryAsync();

            stopwatch.Stop();
            _logger.LogInformation("Updated UploadLog data for file '{FileName}' in {ElapsedMilliseconds} ms", filename, stopwatch.ElapsedMilliseconds);
        }
        private async Task InsertLogDataAsync(
            string tableName,
            string filename,
            string path,
            SqlConnection conn,
            SqlTransaction transaction)
        {
            var stopwatch = Stopwatch.StartNew();
           
            string sql = @"INSERT INTO UploadLog (Path, FileName, TableName, UploadedAt, IsActive) 
                          VALUES (@path, @filename, @tableName, @uploadedAt, 1)";
            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@path", path);
            cmd.Parameters.AddWithValue("@filename", filename);
            cmd.Parameters.AddWithValue("@tableName", tableName);
            cmd.Parameters.AddWithValue("@uploadedAt", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();

            stopwatch.Stop();
            _logger.LogInformation("Inserted log entry for table '{TableName}' and file '{FileName}' in {ElapsedMilliseconds} ms", tableName, filename, stopwatch.ElapsedMilliseconds);
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

}
