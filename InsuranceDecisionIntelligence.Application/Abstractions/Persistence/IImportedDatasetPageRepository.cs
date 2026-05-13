using InsuranceDecisionIntelligence.Application.DTOs.Datasets;

namespace InsuranceDecisionIntelligence.Application.Abstractions.Persistence;

public interface IImportedDatasetPageRepository
{
    Task<ImportedDatasetPageDto> GetPagedRowsAsync(string tableName, int page, int pageSize);
}
