using System.Data;

namespace InsuranceDecisionIntelligence.Application.Abstractions.Data;

public interface IDataReaderSqlBulkImporter
{
    Task ImportAsync(string savedFilePath, string originalFileName, IDataReader reader);
}
