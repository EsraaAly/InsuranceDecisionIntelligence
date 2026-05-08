using InsuranceDecisionIntelligence.Application.Common.Models;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Data
{
    public class BulkInsertService : IBulkInsertService
    {
        private readonly ConnectionSettings _connectionString;

        public BulkInsertService(IOptions<ConnectionSettings> options)
        {
            _connectionString = options.Value;
        }

        // ================= MAIN =================
        //public async Task InsertAsync(string fileName, DataTable dataTable)
        //{
        //    using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
        //    await conn.OpenAsync();

        //    NormalizeColumns(dataTable);

        //    using var transaction = conn.BeginTransaction();

        //    try
        //    {
        //        string baseName = CleanName(Path.GetFileNameWithoutExtension(fileName));
        //        string tableName = $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}";

        //        await CreateTableAsync(tableName, dataTable, conn, transaction);

        //        await InsertDataAsync(tableName, dataTable, conn, transaction);

        //        await transaction.CommitAsync();
        //    }
        //    catch
        //    {
        //        await transaction.RollbackAsync();
        //        throw;
        //    }
        //}

        public async Task InsertAsync(string fileName, IDataReader reader)
        {
            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {

                string baseName = CleanName(Path.GetFileNameWithoutExtension(fileName));
                string tableName = $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}";

                // 1. get columns
                if (!reader.Read())
                    throw new Exception("Empty file");

                var columns = new List<string>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string? value = reader.GetValue(i)?.ToString();

                    if (string.IsNullOrWhiteSpace(value))
                        value = $"Column{i}";

                    columns.Add(CleanName(value));
                }

                // 2. create table
                await CreateTableAsync(tableName, columns, conn, transaction);

                // 3. skip header (Excel first row)
                //reader.Read();

                //4. insert data
                await InsertDataAsync(tableName, reader, columns, conn, transaction);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                await conn.CloseAsync();
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

        // ================= RESOLVE TABLE =================
        //private async Task<string> ResolveTableAsync(
        //    string fileName,
        //    DataTable dataTable,
        //    SqlConnection conn,
        //    SqlTransaction transaction)
        //{
        //    var baseName = CleanName(Path.GetFileNameWithoutExtension(fileName));

        //    string tableName = baseName;
        //    int counter = 1;

        //    while (await TableExistsAsync(conn, tableName))
        //    {

        //        var dbColumns = await GetTableColumnsAsync(conn, tableName);

        //        var dtColumns = new List<string>();
        //        foreach (DataColumn c in dataTable.Columns)
        //            dtColumns.Add(c.ColumnName);

        //        if (IsSchemaSame(dbColumns, dtColumns))
        //        {
        //            await TruncateTableAsync(tableName, conn, transaction);
        //            return tableName;
        //        }

        //        tableName = $"{baseName}_{counter}";
        //        counter++;
        //    }

        //    await CreateTableAsync(tableName, dataTable, conn, transaction);
        //    return tableName;
        //}

        // ================= CREATE TABLE =================
        //private async Task CreateTableAsync(
        //    string tableName,
        //    DataTable dataTable,
        //    SqlConnection conn,
        //    SqlTransaction transaction)
        //{
        //    var sb = new StringBuilder();
        //    bool first = true;

        //    foreach (DataColumn column in dataTable.Columns)
        //    {
        //        if (!first)
        //            sb.Append(",");

        //        sb.Append($"[{column.ColumnName}] NVARCHAR(MAX)");

        //        first = false;
        //    }

        //    var sql = $@"
        //                CREATE TABLE [{tableName}] (
        //                {sb}
        //                )";

        //    using var cmd = new SqlCommand(sql, conn, transaction);
        //    await cmd.ExecuteNonQueryAsync();
        //}
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
                sb.Append($"[{columns[i]}] NVARCHAR(MAX)");
            }

            var sql = $@"
                        CREATE TABLE [{tableName}] (
                        {sb}
                        )";

            using var cmd = new SqlCommand(sql, conn, transaction);
            await cmd.ExecuteNonQueryAsync();
        }

        // ================= BULK INSERT (FAST) =================
        //private async Task InsertDataAsync(
        //    string tableName,
        //    DataTable dataTable,
        //    SqlConnection conn,
        //    SqlTransaction transaction)
        //{
        //    using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, transaction)
        //    {
        //        DestinationTableName = tableName,
        //        BatchSize = 10000,
        //        BulkCopyTimeout = 0
        //    };

        //    foreach (DataColumn column in dataTable.Columns)
        //    {
        //        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        //    }

        //    await bulkCopy.WriteToServerAsync(dataTable);
        //}

        private async Task InsertDataAsync(
                        string tableName,
                        IDataReader reader,
                        List<string> columns,
                        SqlConnection conn,
                        SqlTransaction transaction)
        {
            using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, transaction)
            {
                DestinationTableName = tableName,
                BatchSize = 10000,
                BulkCopyTimeout = 0,
                EnableStreaming = true
            };

            for (int i = 0; i < columns.Count; i++)
            {
                bulkCopy.ColumnMappings.Add(i, columns[i]);
            }

            await bulkCopy.WriteToServerAsync(reader);
        }

        // ================= TABLE EXISTS =================
        //private async Task<bool> TableExistsAsync(SqlConnection conn, string tableName)
        //{
        //    var sql = @"
        //                SELECT 1 
        //                FROM INFORMATION_SCHEMA.TABLES 
        //                WHERE TABLE_NAME = @tableName 
        //                AND TABLE_SCHEMA = 'dbo'";

        //    using var cmd = new SqlCommand(sql, conn);
        //    cmd.Parameters.AddWithValue("@tableName", tableName);

        //    var result = await cmd.ExecuteScalarAsync();
        //    return result != null;
        //}

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

        // ================= TRUNCATE =================
        //private async Task TruncateTableAsync(
        //    string tableName,
        //    SqlConnection conn,
        //    SqlTransaction transaction)
        //{
        //    var sql = $"TRUNCATE TABLE [{tableName}]";

        //    using var cmd = new SqlCommand(sql, conn, transaction);
        //    await cmd.ExecuteNonQueryAsync();
        //}

        ////// ================= SCHEMA COMPARE =================
        //private bool IsSchemaSame(List<string> dbCols, List<string> dtCols)
        //{
        //    var db = new HashSet<string>(dbCols, StringComparer.OrdinalIgnoreCase);
        //    var dt = new HashSet<string>(dtCols, StringComparer.OrdinalIgnoreCase);

        //    return db.SetEquals(dt);
        //}

        ////// ================= GET DB COLUMNS =================
        //private async Task<List<string>> GetTableColumnsAsync(SqlConnection conn, string tableName)
        //{
        //    var columns = new List<string>();

        //    var sql = @"
        //                SELECT COLUMN_NAME 
        //                FROM INFORMATION_SCHEMA.COLUMNS 
        //                WHERE TABLE_NAME = @tableName 
        //                AND TABLE_SCHEMA = 'dbo'";

        //    using var cmd = new SqlCommand(sql, conn);
        //    cmd.Parameters.AddWithValue("@tableName", tableName);

        //    using var reader = await cmd.ExecuteReaderAsync();

        //    while (await reader.ReadAsync())
        //    {
        //        columns.Add(reader.GetString(0));
        //    }

        //    return columns;
        //}
    }
}