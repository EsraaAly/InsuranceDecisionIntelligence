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
        Task<ResultDto> ReadFileAsync(string filePath, int page, int pageSize);

    }
}
