using InsuranceDecisionIntelligence.Application.DTOs.Data;
using System;

namespace InsuranceDecisionIntelligence.Application.Common
{
    public static class ValidationHelper
    {
        public static Result ValidateFileId(int fileId)
        {
            if (fileId <= 0)
                return Result.Failure(Error.Validation("File ID must be positive"));
            
            return Result.Success();
        }

        public static Result ValidatePageParameters(int page, int pageSize)
        {
            if (page <= 0)
                return Result.Failure(Error.Validation("Page number must be positive"));
            
            if (pageSize <= 0 || pageSize > 1000)
                return Result.Failure(Error.Validation("Page size must be between 1 and 1000"));
            
            return Result.Success();
        }

        public static Result ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return Result.Failure(Error.Validation("File path is required"));
            
            return Result.Success();
        }

        public static Result ValidateChartRequest(ChartDataRequestDto request)
        {
            if (request.fileId <= 0)
                return Result.Failure(Error.Validation("File ID must be positive"));
            
            if (string.IsNullOrWhiteSpace(request.XColumn))
                return Result.Failure(Error.Validation("X column is required"));
            
            if (string.IsNullOrWhiteSpace(request.YColumn))
                return Result.Failure(Error.Validation("Y column is required"));
            
            if (string.IsNullOrWhiteSpace(request.Aggregation))
                return Result.Failure(Error.Validation("Aggregation type is required"));
            
            return Result.Success();
        }
    }
}
