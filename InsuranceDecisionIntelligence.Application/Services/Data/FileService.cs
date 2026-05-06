using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Services.Data
{
    public class FileService : IFileService
    {
        private readonly IFileProvider _fileProvider;
        private readonly IFileReader _fileReader;
        private readonly IBulkInsertService _bulkInsertService;

        public FileService(IFileProvider fileProvider, IFileReader fileReader, IBulkInsertService bulkInsertService)
        {
            _fileProvider = fileProvider;
            _fileReader = fileReader;
            _bulkInsertService = bulkInsertService;
        }

        public async Task<string> ProcessFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File is empty");

            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));

            var folderPath = Path.Combine(
                basePath,
                "InsuranceDecisionIntelligence.Infrastructure",
                "FileStorage",
                "Uploads"
            );

            await _fileProvider.CreateDirectoryAsync(folderPath);

            string fullPath = await _fileProvider.SaveAync(file, folderPath);

            return fullPath;
        }
    }
}
