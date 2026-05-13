using InsuranceDecisionIntelligence.Application.Common;
using InsuranceDecisionIntelligence.Application.DTOs.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Services.Data
{
    public class MetadataAnalysisService : IMetadataAnalysisService
    {
        private readonly IDatabaseMetaDataService _databaseMetaDataService;
        private readonly ILogger<MetadataAnalysisService> _logger;

        public MetadataAnalysisService(IDatabaseMetaDataService databaseMetaDataService, ILogger<MetadataAnalysisService> logger)
        {
            _databaseMetaDataService = databaseMetaDataService;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<dynamic>>> AnalyzeMetadataAsync(ChartDataRequestDto dto)
        {
            string tableName = await _databaseMetaDataService.GetTableNameByIdAsync(dto.fileId);
            var stopwatch = Stopwatch.StartNew();
            
            var result = await _databaseMetaDataService.GetDynamicChartData(tableName, dto.XColumn, dto.YColumn, dto.Aggregation, dto.Top10Only);
            
            stopwatch.Stop();
            _logger.LogInformation("Metadata analysis completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            
            return Result.Success(result);
        }
    }
}
