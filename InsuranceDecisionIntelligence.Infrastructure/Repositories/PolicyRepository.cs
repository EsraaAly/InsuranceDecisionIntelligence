using Dapper;
using DocumentFormat.OpenXml.Wordprocessing;
using InsuranceDecisionIntelligence.Application.Common.Models;
using InsuranceDecisionIntelligence.Application.DTOs.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Repositories
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly ConnectionSettings _connectionString;
        private readonly ILogger<PolicyRepository> _logger;

        public PolicyRepository(IOptions<ConnectionSettings> options, ILogger<PolicyRepository> logger)
        {
            _connectionString = options.Value;
            _logger = logger;
        }

        //by Dapper
        public async Task<GetDataResponse> GetDataAsync(string tableName, int page, int pageSize)
        {
            //var skip = (page - 1) * pageSize;
            //var take = pageSize;

            //using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
            //string query = $"Select * from [{tableName}] order by TableId offset {skip} rows fetch next {take} rows only";


            //var result = await conn.QueryAsync(query);
            //var count_rows = result.Count();
            //return new GetFileDetails
            //{
            //    Data = result,
            //    Count = count_rows
            //};
            var skip = (page - 1) * pageSize;
            var take = pageSize;

            // كود SQL محسّن بيجيب الـ Count والـ Data في خبطة واحدة
            string countQuery = $@"SELECT SUM(rows) FROM sys.partitions 
                       WHERE object_id = OBJECT_ID('{tableName}') 
                       AND index_id < 2;";

            //string dataQuery = $@"SELECT * FROM [{tableName}] 
            //          ORDER BY TableId 
            //          OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";

            string dataQuery = $"SELECT TOP {take} * FROM {tableName} WHERE TableId > {skip} ORDER BY TableId;";

            string finalQuery = countQuery + dataQuery;

            //string query = $@"
            //                SELECT COUNT(*) FROM [{tableName}];
            //                SELECT * FROM [{tableName}] 
            //                ORDER BY TableId 
            //                OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";

            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);

            // استخدام QueryMultipleAsync لتقليل الـ Round trips لقاعدة البيانات
            using var multi = await conn.QueryMultipleAsync(finalQuery);

            // السطر ده هيجيب الـ Count من أول Select في الكويري
            var totalCount = await multi.ReadFirstAsync<int>();

            // السطر ده هيجيب الـ 1000 سجل بتوعك
            var result = await multi.ReadAsync<dynamic>();

            return new GetDataResponse
            {
                Data = result,
                Count = totalCount // كدة الـ Count بقى دقيق وشامل كل الجدول
            };
        }

        //sql data reader
        //public async Task<ResultDto> GetDataAsync(string tableName, int page, int pageSize)
        //{
        //    var skip = (page - 1) * pageSize;
        //    var take = pageSize;

        //    var result = new List<dynamic>();
        //    using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);
        //    await conn.OpenAsync();

        //    string query = $"Select * from [{tableName}] order by id offset {skip} rows fetch next {take} rows only";

        //    SqlCommand cmd = new SqlCommand(query, conn);

        //    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        //    List<string> columns = new List<string>(reader.FieldCount);

        //    for (var i = 0;i< reader.FieldCount; i++)
        //    {
        //        columns.Add(reader.GetName(i));
        //    }
        //    while (await reader.ReadAsync())
        //    {
        //        var row = new Dictionary<string, object>(reader.FieldCount);

        //        for (var i = 0; i < reader.FieldCount; i++)
        //        {
        //            row[columns[i]] = reader.GetValue(i);
        //        }
        //        result.Add(row);
        //    }
        //    var count_rows = result.Count();
        //    return new ResultDto
        //    {
        //        Data = result,
        //        Count = count_rows
        //    };
        //}
    }
}
