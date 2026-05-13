using System.Data;

namespace InsuranceDecisionIntelligence.Application.Abstractions.Data;

public interface IDataTableSqlBulkImporter
{
    Task ImportAsync(string originalFileName, DataTable dataTable);
}
