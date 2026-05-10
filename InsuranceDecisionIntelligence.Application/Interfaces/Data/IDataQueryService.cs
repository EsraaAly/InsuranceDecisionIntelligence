using InsuranceDecisionIntelligence.Application.DTOs.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.Data
{
    public interface IDataQueryService
    {
        Task<GetFileDetails> ReadFileDataAsync(string filePath, int page, int pageSize);

        Task<IEnumerable<GetUploadedFilesDto>> GetAllFilesAsync();

        Task<GetFileDetails> GetFileDetailsByIdAsync(int id, int page=1, int pageSize=100);
    }
}
