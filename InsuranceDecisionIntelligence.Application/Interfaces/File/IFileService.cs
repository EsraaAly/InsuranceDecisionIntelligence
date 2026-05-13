using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.File
{
    public interface IFileService
    {
        //Task<List<Dictionary<string, object>>> ReadFileUltraFastAsync(string filePath, int page, int pageSize = 1000);
        //Task<List<Dictionary<string, object>>> ReadFilePartialAsync(string filePath, long startByte, long endByte, int page, int pageSize = 1000);
        //Task<List<Dictionary<string, object>>> ReadFirstPageAsync(string filePath);

        Task<string> ProcessFile(IFormFile file);
    }
}
