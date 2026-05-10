using InsuranceDecisionIntelligence.Application.DTOs.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.Repositories
{
    public interface IPolicyRepository
    {
        Task<GetFileDetails> GetDataAsync(string tableName,int page,int pageSize);
    }
}
