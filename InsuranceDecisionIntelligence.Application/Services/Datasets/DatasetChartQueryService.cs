using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Common;
using InsuranceDecisionIntelligence.Application.DTOs.Datasets;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InsuranceDecisionIntelligence.Application.Services.Datasets;

public class DatasetChartQueryService : IDatasetChartQueryService
{
    private readonly IUploadDatasetMetadataReader _uploadMetadataReader;
    private readonly ILogger<DatasetChartQueryService> _logger;

    public DatasetChartQueryService(
        IUploadDatasetMetadataReader uploadMetadataReader,
        ILogger<DatasetChartQueryService> logger)
    {
        _uploadMetadataReader = uploadMetadataReader;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<dynamic>>> GetAggregatedChartAsync(DatasetChartQueryRequest request)
    {
        string? tableName = await _uploadMetadataReader.GetTableNameByUploadIdAsync(request.FileId);
        if (string.IsNullOrWhiteSpace(tableName))
            return Result.Failure<IEnumerable<dynamic>>(Error.NotFound($"No imported dataset found for upload id {request.FileId}"));

        var stopwatch = Stopwatch.StartNew();

        var result = await _uploadMetadataReader.GetDynamicChartData(
            tableName,
            request.XColumn,
            request.YColumn,
            request.Aggregation,
            request.Top10Only);

        stopwatch.Stop();
        _logger.LogInformation("Dataset chart query completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

        return Result.Success(result);
    }
}
