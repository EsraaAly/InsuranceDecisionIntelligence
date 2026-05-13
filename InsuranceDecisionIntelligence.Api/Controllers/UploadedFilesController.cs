using InsuranceDecisionIntelligence.Api.Common;
using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Abstractions.File;
using InsuranceDecisionIntelligence.Application.DTOs.Datasets;
using InsuranceDecisionIntelligence.Application.DTOs.Uploads;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InsuranceDecisionIntelligence.Api.Controllers;

[Route("api/File")]
[ApiController]
public class UploadedFilesController : ControllerBase
{
    private readonly IFileUploadImportService _fileUploadImportService;
    private readonly IImportedDatasetQueryService _importedDatasetQueryService;
    private readonly IDatasetChartQueryService _datasetChartQueryService;

    public UploadedFilesController(
        IFileUploadImportService fileUploadImportService,
        IImportedDatasetQueryService importedDatasetQueryService,
        IDatasetChartQueryService datasetChartQueryService)
    {
        _fileUploadImportService = fileUploadImportService;
        _importedDatasetQueryService = importedDatasetQueryService;
        _datasetChartQueryService = datasetChartQueryService;
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetUploadedSummariesAsync()
    {
        var result = await _importedDatasetQueryService.GetUploadedFileSummariesAsync();
        return result.ToHttpResponse();
    }

    [HttpGet("preview")]
    public async Task<IActionResult> GetImportedDatasetPageAsync([FromQuery] int id, [FromQuery] int pageNo, [FromQuery] int pageSize)
    {
        var result = await _importedDatasetQueryService.GetImportedDatasetPageAsync(id, pageNo, pageSize);
        return result.ToHttpResponse();
    }

    [HttpGet("jobs")]
    public IActionResult GetBackgroundJobs()
    {
        var jobs = new[]
        {
            new { FileName = "sales_2024_05.csv", Status = "Processing", Progress = 75, StartedAt = "10:15 AM" },
            new { FileName = "orders_may.csv", Status = "Queued", Progress = 0, StartedAt = "10:20 AM" }
        };
        return Ok(jobs);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(long.MaxValue)]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> UploadAsync([FromForm] UploadFileRequest dto)
    {
        var stopwatch = Stopwatch.StartNew();
        string filepath = await _fileUploadImportService.SaveAndQueueImportAsync(dto.File);
        stopwatch.Stop();

        return Ok(new
        {
            Data = filepath,
            TimeTaken = stopwatch.ElapsedMilliseconds
        });
    }

    [HttpPost("analyze/metadata")]
    public async Task<IActionResult> GetAggregatedChartAsync([FromBody] DatasetChartQueryRequest request)
    {
        var result = await _datasetChartQueryService.GetAggregatedChartAsync(request);
        return result.ToHttpResponse();
    }
}
