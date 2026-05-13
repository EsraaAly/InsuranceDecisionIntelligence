using InsuranceDecisionIntelligence.Application.Common;
using InsuranceDecisionIntelligence.Application.DTOs.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.Data
{
    public interface IMetadataAnalysisService
    {
        Task<Result<IEnumerable<dynamic>>> AnalyzeMetadataAsync(ChartDataRequestDto dto);
    }
}
