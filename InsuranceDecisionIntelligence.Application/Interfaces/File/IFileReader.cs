using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.File
{
    public interface IFileReader
    {
        //Task<List<Dictionary<string, object>>> ReadFileAsync(string filePath, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<DataTable> ReadAsDataTableAsync(string filePath);
        Task<IDataReader> ReadAsDataReaderAsync(string filePath);
    }
}
