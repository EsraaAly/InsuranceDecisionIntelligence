using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.Data
{
    public interface IFileReader
    {
        Task<List<Dictionary<string, object>>> ReadFileAsync(string filePath, int page, int pageSize, CancellationToken cancellationToken = default);

    }
}
