using InsuranceDecisionIntelligence.Application.Common;
using InsuranceDecisionIntelligence.Application.DTOs.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.File;
using InsuranceDecisionIntelligence.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Services.Data
{
    public class DataQueryService : IDataQueryService
    {
        private readonly IDatabaseMetaDataService _databaseMetaDataService;
        private readonly IPolicyRepository _policyRepository;
        private readonly ILogger<DataQueryService> _logger;

        public DataQueryService(IDatabaseMetaDataService databaseMetaDataService,
                                IPolicyRepository policyRepository,
                                ILogger<DataQueryService> logger)
        {
            _databaseMetaDataService = databaseMetaDataService;
            _policyRepository = policyRepository;
            _logger = logger;
        }
        public async Task<Result<IEnumerable<GetUploadedFilesDto>>> GetAllFilesAsync()
        {
            try
            {
                var swGetTable = Stopwatch.StartNew();
                var files = await _databaseMetaDataService.GetUploadedFilesAsync();
                swGetTable.Stop();
                _logger.LogInformation("Retrieved all uploaded files in {ElapsedMilliseconds} ms", swGetTable.ElapsedMilliseconds);

                return Result.Success(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all uploaded files");
                return Result.Failure<IEnumerable<GetUploadedFilesDto>>(Error.Internal("Failed to retrieve uploaded files", new Dictionary<string, object> { ["exception"] = ex.Message }));
            }
        }

        public async Task<Result<GetDataResponse>> GetFileDetailsByIdAsync(int id, int page = 1, int pageSize = 100)
        {
            try
            {
                var validationResult = ValidateGetFileDetailsRequest(id, page, pageSize);
                if (validationResult.IsFailure)
                    return Result.Failure<GetDataResponse>(validationResult.Error);

                var tableDetails = await GetTableDetailsAsync(id);
                if (tableDetails.IsFailure)
                    return Result.Failure<GetDataResponse>(tableDetails.Error);

                var dataResult = await GetTableDataAsync(tableDetails.Value.TableName, page, pageSize);
                if (dataResult.IsFailure)
                    return Result.Failure<GetDataResponse>(dataResult.Error);

                return AssembleFileDetailsResult(dataResult.Value, tableDetails.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file details for ID: {FileId}", id);
                return Result.Failure<GetDataResponse>(Error.Internal("Failed to get file details", new Dictionary<string, object> { ["fileId"] = id, ["exception"] = ex.Message }));
            }
        }

        private Result ValidateGetFileDetailsRequest(int id, int page, int pageSize)
        {
            var fileIdValidation = ValidationHelper.ValidateFileId(id);
            if (fileIdValidation.IsFailure)
                return fileIdValidation;

            return ValidationHelper.ValidatePageParameters(page, pageSize);
        }

        private async Task<Result<TableDetails>> GetTableDetailsAsync(int fileId)
        {
            var stopwatch = Stopwatch.StartNew();
            var dictionary = await _databaseMetaDataService.GetTableDetailsByIdAsync(fileId);
            stopwatch.Stop();
            
            //_logger.LogInformation("Retrieved table details for file ID {FileId} in {ElapsedMilliseconds} ms", fileId, stopwatch.ElapsedMilliseconds);

            if (dictionary == null || !dictionary.ContainsKey("TableName"))
                return Result.Failure<TableDetails>(Error.NotFound($"File with ID {fileId} not found"));

            return Result.Success(new TableDetails
            {
                TableName = dictionary["TableName"].ToString(),
                UploadedDate = (DateTime)dictionary["UploadedDate"],
                RowsCount = (long)dictionary["RowsCount"],
                ColumnsCount = (int)dictionary["ColumnsCount"]
            });
        }

        private async Task<Result<GetDataResponse>> GetTableDataAsync(string tableName, int page, int pageSize)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _policyRepository.GetDataAsync(tableName, page, pageSize);
            stopwatch.Stop();
            
            _logger.LogInformation("Retrieved data from table '{TableName}' (page {Page}, size {PageSize}) in {ElapsedMilliseconds} ms", tableName, page, pageSize, stopwatch.ElapsedMilliseconds);

            return Result.Success(result);
        }

        private static Result<GetDataResponse> AssembleFileDetailsResult(GetDataResponse dataResponse, TableDetails tableDetails)
        {
            dataResponse.UploadedDate = tableDetails.UploadedDate;
            dataResponse.RowsCount = tableDetails.RowsCount;
            dataResponse.ColumnsCount = tableDetails.ColumnsCount;
            
            return Result.Success(dataResponse);
        }

        private class TableDetails
        {
            public string TableName { get; set; }
            public DateTime UploadedDate { get; set; }
            public long RowsCount { get; set; }
            public int ColumnsCount { get; set; }
        }

    }
}
