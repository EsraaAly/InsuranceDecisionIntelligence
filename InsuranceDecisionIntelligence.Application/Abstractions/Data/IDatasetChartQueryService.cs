using InsuranceDecisionIntelligence.Application.Common;
using InsuranceDecisionIntelligence.Application.DTOs.Datasets;

namespace InsuranceDecisionIntelligence.Application.Abstractions.Data;

public interface IDatasetChartQueryService
{
    Task<Result<IEnumerable<dynamic>>> GetAggregatedChartAsync(DatasetChartQueryRequest request);
}
