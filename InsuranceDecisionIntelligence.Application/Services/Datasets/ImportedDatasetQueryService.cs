using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Abstractions.Persistence;
using InsuranceDecisionIntelligence.Application.Common;
using InsuranceDecisionIntelligence.Application.DTOs.Datasets;
using InsuranceDecisionIntelligence.Application.DTOs.Uploads;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InsuranceDecisionIntelligence.Application.Services.Datasets;

public class ImportedDatasetQueryService : IImportedDatasetQueryService
{
    private readonly IUploadDatasetMetadataReader _uploadMetadataReader;
    private readonly IImportedDatasetPageRepository _datasetPageRepository;
    private readonly ILogger<ImportedDatasetQueryService> _logger;

    public ImportedDatasetQueryService(
        IUploadDatasetMetadataReader uploadMetadataReader,
        IImportedDatasetPageRepository datasetPageRepository,
        ILogger<ImportedDatasetQueryService> logger)
    {
        _uploadMetadataReader = uploadMetadataReader;
        _datasetPageRepository = datasetPageRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<UploadedFileSummaryDto>>> GetUploadedFileSummariesAsync()
    {
        try
        {
            var swGetTable = Stopwatch.StartNew();
            var files = await _uploadMetadataReader.GetUploadedFileSummariesAsync();
            swGetTable.Stop();
            _logger.LogInformation("Retrieved all uploaded files in {ElapsedMilliseconds} ms", swGetTable.ElapsedMilliseconds);

            return Result.Success(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all uploaded files");
            return Result.Failure<IEnumerable<UploadedFileSummaryDto>>(Error.Internal("Failed to retrieve uploaded files", new Dictionary<string, object> { ["exception"] = ex.Message }));
        }
    }

    public async Task<Result<ImportedDatasetPageDto>> GetImportedDatasetPageAsync(int uploadId, int page = 1, int pageSize = 100)
    {
        try
        {
            var validationResult = ValidateGetImportedDatasetPageRequest(uploadId, page, pageSize);
            if (validationResult.IsFailure)
                return Result.Failure<ImportedDatasetPageDto>(validationResult.Error);

            var tableDetails = await GetTableDetailsAsync(uploadId);
            if (tableDetails.IsFailure)
                return Result.Failure<ImportedDatasetPageDto>(tableDetails.Error);

            var dataResult = await GetTableDataAsync(tableDetails.Value.TableName, page, pageSize);
            if (dataResult.IsFailure)
                return Result.Failure<ImportedDatasetPageDto>(dataResult.Error);

            return AssembleImportedDatasetPageResult(dataResult.Value, tableDetails.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file details for ID: {FileId}", uploadId);
            return Result.Failure<ImportedDatasetPageDto>(Error.Internal("Failed to get file details", new Dictionary<string, object> { ["fileId"] = uploadId, ["exception"] = ex.Message }));
        }
    }

    private Result ValidateGetImportedDatasetPageRequest(int uploadId, int page, int pageSize)
    {
        var fileIdValidation = ValidationHelper.ValidateFileId(uploadId);
        if (fileIdValidation.IsFailure)
            return fileIdValidation;

        return ValidationHelper.ValidatePageParameters(page, pageSize);
    }

    private async Task<Result<TableDetails>> GetTableDetailsAsync(int fileId)
    {
        var stopwatch = Stopwatch.StartNew();
        var dictionary = await _uploadMetadataReader.GetTableDetailsByUploadIdAsync(fileId);
        stopwatch.Stop();

        if (dictionary == null || !dictionary.ContainsKey("TableName"))
            return Result.Failure<TableDetails>(Error.NotFound($"File with ID {fileId} not found"));

        return Result.Success(new TableDetails
        {
            TableName = dictionary["TableName"].ToString()!,
            UploadedDate = (DateTime)dictionary["UploadedDate"],
            RowsCount = (long)dictionary["RowsCount"],
            ColumnsCount = (int)dictionary["ColumnsCount"]
        });
    }

    private async Task<Result<ImportedDatasetPageDto>> GetTableDataAsync(string tableName, int page, int pageSize)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _datasetPageRepository.GetPagedRowsAsync(tableName, page, pageSize);
        stopwatch.Stop();

        _logger.LogInformation("Retrieved data from table '{TableName}' (page {Page}, size {PageSize}) in {ElapsedMilliseconds} ms", tableName, page, pageSize, stopwatch.ElapsedMilliseconds);

        return Result.Success(result);
    }

    private static Result<ImportedDatasetPageDto> AssembleImportedDatasetPageResult(ImportedDatasetPageDto dataResponse, TableDetails tableDetails)
    {
        dataResponse.UploadedDate = tableDetails.UploadedDate;
        dataResponse.RowsCount = tableDetails.RowsCount;
        dataResponse.ColumnsCount = tableDetails.ColumnsCount;

        return Result.Success(dataResponse);
    }

    private sealed class TableDetails
    {
        public string TableName { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public long RowsCount { get; set; }
        public int ColumnsCount { get; set; }
    }
}
