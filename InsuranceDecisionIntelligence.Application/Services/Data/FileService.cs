using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
        private readonly ILogger<FileService> _logger;

        public FileService(IFileProvider fileProvider, IFileReader fileReader, IBulkInsertService bulkInsertService,ILogger<FileService> logger)
        {
            _fileProvider = fileProvider;
            _fileReader = fileReader;
            _bulkInsertService = bulkInsertService;
            _logger = logger;
        }

        public async Task<string> ProcessFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));

            var folderPath = Path.Combine(
                basePath,
                "InsuranceDecisionIntelligence.Infrastructure",
                "FileStorage",
                "Uploads"
            );

            await _fileProvider.CreateDirectoryAsync(folderPath);

            string fullPath = await _fileProvider.SaveAync(file, folderPath);

            _ = Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();

                var dataTable = await _fileReader.ReadAsDataTableAsync(fullPath);
                await _bulkInsertService.InsertAsync(file.FileName, dataTable);

                sw.Stop();

                _logger.LogInformation("Processing time: {ms} ms", sw.ElapsedMilliseconds);
            });
            //using var dataTable = await _fileReader.ReadAsDataTableAsync(fullPath);
            //await _bulkInsertService.InsertAsync(file.FileName, dataTable);

            return fullPath;
        }
    }
}
