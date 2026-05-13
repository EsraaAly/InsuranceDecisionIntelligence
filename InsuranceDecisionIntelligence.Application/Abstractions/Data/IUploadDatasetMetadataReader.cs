using InsuranceDecisionIntelligence.Application.DTOs.Uploads;

namespace InsuranceDecisionIntelligence.Application.Abstractions.Data;

public interface IUploadDatasetMetadataReader
{
    Task<IEnumerable<dynamic>> GetDynamicChartData(
        string tableName,
        string xAxisColumn,
        string yAxisColumn,
        string aggregationType,
        bool top10Only);

    Task<string?> GetTableNameByFilePathAsync(string filePath);
    Task<string?> GetTableNameByUploadIdAsync(int uploadId);
    Task<IEnumerable<UploadedFileSummaryDto>> GetUploadedFileSummariesAsync();
    Task<Dictionary<string, object>> GetTableDetailsByUploadIdAsync(int uploadId);
    Task<bool> TableExistsAsync(string tableName);
}
