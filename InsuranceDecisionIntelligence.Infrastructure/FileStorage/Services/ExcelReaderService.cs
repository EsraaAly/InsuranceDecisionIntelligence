using ExcelDataReader;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.FileStorage.Services
{
    public class ExcelReaderService : IFileReader
    {
        //public async Task<List<Dictionary<string, object>>> ReadFileAsync(string filePath, int page, int pageSize = 1000, CancellationToken cancellationToken = default)
        //{
        //    var startRow = 2 + ((page - 1) * pageSize);
        //    var endRow = startRow + pageSize - 1;

        //    // Use synchronous I/O for maximum speed
        //    return await Task.Run(() =>
        //    {
        //        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920   , FileOptions.SequentialScan);
        //        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration()
        //        {
        //            FallbackEncoding = System.Text.Encoding.UTF8,
        //            AutodetectSeparators = Array.Empty<char>(),
        //            LeaveOpen = false
        //        });

        //        // Read headers
        //        reader.Read(); // Skip header row
        //        var fieldCount = reader.FieldCount;
        //        var headers = new string[fieldCount];
        //        for (var col = 0; col < fieldCount; col++)
        //        {
        //            headers[col] = reader.GetString(col) ?? $"Column{col}";
        //        }

        //        // Fast skip to start row
        //        var currentRow = 1;
        //        for (; currentRow < startRow && reader.Read(); currentRow++) { }

        //        // Process target rows
        //        var result = new List<Dictionary<string, object>>(pageSize);
        //        for (; currentRow <= endRow && reader.Read(); currentRow++)
        //        {
        //            var rowDict = new Dictionary<string, object>(fieldCount);
        //            for (var col = 0; col < fieldCount; col++)
        //            {
        //                var value = reader.GetValue(col);
        //                if (value != DBNull.Value && value != null)
        //                {
        //                    rowDict[headers[col]] = value;
        //                }
        //            }
        //            result.Add(rowDict);
        //        }

        //        return result;
        //    }, cancellationToken);
        //}

        //public async Task<List<Dictionary<string, object>>> ReadFileUltraFastAsync(string filePath, int page, int pageSize = 1000)
        //{
        //    var startRow = 2 + ((page - 1) * pageSize);
        //    var endRow = startRow + pageSize - 1;

        //    return await Task.Run(() =>
        //    {
        //        // Extreme buffer size + OS caching
        //        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1048576, FileOptions.SequentialScan);

        //        // Zero-config ExcelReader for max speed
        //        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration()
        //        {
        //            AutodetectSeparators = Array.Empty<char>(),
        //            LeaveOpen = false
        //        });

        //        // Headers - single pass
        //        reader.Read();
        //        var fc = reader.FieldCount;
        //        var h = new string[fc];
        //        for (var i = 0; i < fc; i++) h[i] = reader.GetString(i) ?? $"C{i}";

        //        // Result with exact capacity
        //        var r = new List<Dictionary<string, object>>(pageSize);

        //        // Skip rows - minimal loop
        //        var cr = 1;
        //        while (cr < startRow && reader.Read()) cr++;

        //        // Process rows - ultra optimized
        //        while (cr <= endRow && reader.Read())
        //        {
        //            var d = new Dictionary<string, object>(fc);
        //            for (var i = 0; i < fc; i++)
        //            {
        //                var v = reader.GetValue(i);
        //                if (v != null && v != DBNull.Value) d[h[i]] = v;
        //            }
        //            r.Add(d);
        //            cr++;
        //        }

        //        return r;
        //    });
        //}

        //public async Task<List<Dictionary<string, object>>> ReadFilePartialAsync(string filePath, long startByte, long endByte, int page, int pageSize = 1000)
        //{
        //    var startRow = 2 + ((page - 1) * pageSize);
        //    var endRow = startRow + pageSize - 1;

        //    return await Task.Run(() =>
        //    {
        //        // Read only specific byte range
        //        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1048576, FileOptions.SequentialScan);

        //        // Seek to start position
        //        if (startByte > 0)
        //        {
        //            stream.Seek(startByte, SeekOrigin.Begin);
        //        }

        //        // Calculate max bytes to read
        //        var maxBytes = endByte - startByte;
        //        var buffer = new byte[Math.Min(maxBytes, 1048576)];

        //        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration()
        //        {
        //            AutodetectSeparators = Array.Empty<char>(),
        //            LeaveOpen = false
        //        });

        //        // Headers
        //        reader.Read();
        //        var fc = reader.FieldCount;
        //        var h = new string[fc];
        //        for (var i = 0; i < fc; i++) h[i] = reader.GetString(i) ?? $"C{i}";

        //        var r = new List<Dictionary<string, object>>(pageSize);
        //        var cr = 1;

        //        while (cr < startRow && reader.Read()) cr++;
        //        while (cr <= endRow && reader.Read())
        //        {
        //            var d = new Dictionary<string, object>(fc);
        //            for (var i = 0; i < fc; i++)
        //            {
        //                var v = reader.GetValue(i);
        //                if (v != null && v != DBNull.Value) d[h[i]] = v;
        //            }
        //            r.Add(d);
        //            cr++;
        //        }

        //        return r;
        //    });
        //}

        //public async Task<List<Dictionary<string, object>>> ReadFirstPageAsync(string filePath)
        //{
        //    return await Task.Run(() =>
        //    {
        //        // Maximum performance FileStream for first page only
        //        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 2097152, FileOptions.SequentialScan);

        //        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration()
        //        {
        //            AutodetectSeparators = Array.Empty<char>(),
        //            LeaveOpen = false
        //        });

        //        // Read headers
        //        reader.Read();
        //        var fc = reader.FieldCount;
        //        var h = new string[fc];
        //        for (var i = 0; i < fc; i++) h[i] = reader.GetString(i) ?? $"C{i}";

        //        // Pre-allocate exactly 1000 rows
        //        var r = new List<Dictionary<string, object>>(1000);

        //        // Read exactly 1000 rows (page 1 starts at row 2)
        //        for (var row = 0; row < 1000 && reader.Read(); row++)
        //        {
        //            var d = new Dictionary<string, object>(fc);
        //            for (var i = 0; i < fc; i++)
        //            {
        //                var v = reader.GetValue(i);
        //                if (v != null && v != DBNull.Value) d[h[i]] = v;
        //            }
        //            r.Add(d);
        //        }

        //        return r;
        //    });
        //}

        public Task<IDataReader> ReadAsDataReaderAsync(string filePath)
        {
            var stream = OpenFileStream(filePath);

            var reader = ExcelReaderFactory.CreateCsvReader(stream);
            reader.Read();

            return Task.FromResult((IDataReader)reader);
        }

        private FileStream OpenFileStream(string filePath)
        {
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                1048576,
                FileOptions.SequentialScan);
        }

        public async Task<DataTable> ReadAsDataTableAsync(string filePath)
        {
            var stream = OpenFileStream(filePath);

            var reader = ExcelReaderFactory.CreateCsvReader(stream);

            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });

            var table = result.Tables[0];

            return table;
        }
    }
}