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
        public async Task<GetFileDetails> GetDataAsync(string tableName, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var take = pageSize;

            using SqlConnection conn = new SqlConnection(_connectionString.DefaultConnection);

            string query = $"Select * from [{tableName}] order by TableId offset {skip} rows fetch next {take} rows only";


            var result = await conn.QueryAsync(query);
            var count_rows = result.Count();
            return new GetFileDetails
            {
                Data = result,
                Count = count_rows
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
