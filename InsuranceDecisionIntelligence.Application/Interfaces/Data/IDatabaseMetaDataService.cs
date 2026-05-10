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
        //public Task CreateTableAsync(
        //    string tableName,            
        //    SqlConnection conn,
        //    SqlTransaction transaction,
        //    DataTable dataTable = null,
        //    List<string> columns = null);

        public Task<string> GetTableNameByPath(string Path);
        public Task<string> GetTableNameById(int Id);
        public Task<IEnumerable<GetUploadedFilesDto>> GetUploadedFilesAsync();
        public Task<Dictionary<string, object>> GetTableDetailsByIdAsync(int Id);
        public Task<bool> TableExistsAsync(string tableName);

    }
}
