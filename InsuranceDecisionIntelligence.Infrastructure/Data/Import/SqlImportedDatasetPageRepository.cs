using Dapper;
using InsuranceDecisionIntelligence.Application.Abstractions.Persistence;
using InsuranceDecisionIntelligence.Application.Configuration;
using InsuranceDecisionIntelligence.Application.DTOs.Datasets;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace InsuranceDecisionIntelligence.Infrastructure.Data.Import;

public class SqlImportedDatasetPageRepository : IImportedDatasetPageRepository
{
    private readonly DatabaseConnectionOptions _connectionOptions;

    public SqlImportedDatasetPageRepository(IOptions<DatabaseConnectionOptions> options)
    {
        _connectionOptions = options.Value;
    }

    public async Task<ImportedDatasetPageDto> GetPagedRowsAsync(string tableName, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var take = pageSize;

        string countQuery = $@"SELECT SUM(rows) FROM sys.partitions 
                       WHERE object_id = OBJECT_ID('{tableName}') 
                       AND index_id < 2;";

        string dataQuery = $"SELECT TOP {take} * FROM {tableName} WHERE TableId > {skip} ORDER BY TableId;";

        string finalQuery = countQuery + dataQuery;

        using SqlConnection conn = new(_connectionOptions.DefaultConnection);
        using var multi = await conn.QueryMultipleAsync(finalQuery);

        var totalCount = await multi.ReadFirstAsync<int>();
        var result = await multi.ReadAsync<dynamic>();

        return new ImportedDatasetPageDto
        {
            Data = result,
            Count = totalCount
        };
    }
}
