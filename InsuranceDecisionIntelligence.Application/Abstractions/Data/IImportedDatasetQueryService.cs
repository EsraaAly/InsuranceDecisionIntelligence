using InsuranceDecisionIntelligence.Application.Common;
using InsuranceDecisionIntelligence.Application.DTOs.Datasets;
using InsuranceDecisionIntelligence.Application.DTOs.Uploads;

namespace InsuranceDecisionIntelligence.Application.Abstractions.Data;

public interface IImportedDatasetQueryService
{
    Task<Result<IEnumerable<UploadedFileSummaryDto>>> GetUploadedFileSummariesAsync();

    Task<Result<ImportedDatasetPageDto>> GetImportedDatasetPageAsync(int uploadId, int page = 1, int pageSize = 100);
}
