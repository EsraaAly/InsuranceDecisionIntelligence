using InsuranceDecisionIntelligence.Application.DTOs.Data;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.Data
{
    public interface IDatabaseMetaDataService
    {
        public Task<IEnumerable<dynamic>> GetDynamicChartData(
                                                                string tableName,
                                                                string xAxisColumn,
                                                                string yAxisColumn,
                                                                string aggregationType,
                                                                bool top10Only);
        public Task<string> GetTableNameByPathAsync(string Path);
        public Task<string> GetTableNameByIdAsync(int Id);
        public Task<IEnumerable<GetUploadedFilesDto>> GetUploadedFilesAsync();
        public Task<Dictionary<string, object>> GetTableDetailsByIdAsync(int Id);
        public Task<bool> TableExistsAsync(string tableName);

    }
}
