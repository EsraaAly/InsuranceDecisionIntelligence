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

        public async Task<ResultDto> ReadFileAsync(string filePath, int page, int pageSize)
        {
            //return tablename
            var swGetTable = Stopwatch.StartNew();
            string tableName = await _databaseMetaDataService.GetTableName(filePath);
            swGetTable.Stop();
            _logger.LogInformation("Get Table: {ms}", swGetTable.ElapsedMilliseconds);

            //return data
            var swGetData = Stopwatch.StartNew();

            var result = await _policyRepository.GetDataAsync(tableName, page, pageSize);
            swGetData.Stop();
            _logger.LogInformation("Get Data: {ms}", swGetData.ElapsedMilliseconds);

            return result;
        }
    }
}
