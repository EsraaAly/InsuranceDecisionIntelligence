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
        private readonly IBulkInsertByIDataReaderService _bulkInsertByIDataReaderService;
        private readonly IBulkInsertByDataTableService _bulkInsertByDataTableService;
        private readonly ILogger<FileService> _logger;

        public FileService(IFileProvider fileProvider,
                           IFileReader fileReader, 
                           IBulkInsertByIDataReaderService bulkInsertByIDataReaderService,
                           IBulkInsertByDataTableService bulkInsertByDataTableService,
                           ILogger<FileService> logger)
        {
            _fileProvider = fileProvider;
            _fileReader = fileReader;
            _bulkInsertByIDataReaderService = bulkInsertByIDataReaderService;
            _logger = logger;
            _bulkInsertByDataTableService = bulkInsertByDataTableService;
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

                //////////////////////////////By Data Table//////////////////////////////////

                //var swRead = Stopwatch.StartNew();
                //var dataTable = await _fileReader.ReadAsDataTableAsync(fullPath);
                //swRead.Stop();
                //_logger.LogInformation("Read: {ms}", swRead.ElapsedMilliseconds);


                //var swInsert = Stopwatch.StartNew();
                //await _bulkInsertByDataTableService.InsertAsync(file.FileName, dataTable);
                //swInsert.Stop();
                //_logger.LogInformation("Insert TOTAL: {ms}", swInsert.ElapsedMilliseconds);

                ////////////////////////////////By Data Reader////////////////////////////////

                var swRead = Stopwatch.StartNew();
                var reader = await _fileReader.ReadAsDataReaderAsync(fullPath);
                swRead.Stop();
                _logger.LogInformation("Read: {ms}", swRead.ElapsedMilliseconds);

                var swInsert = Stopwatch.StartNew();
                await _bulkInsertByIDataReaderService.InsertAsync(fullPath, file.FileName, reader);

                swInsert.Stop();
                _logger.LogInformation("Insert TOTAL: {ms}", swInsert.ElapsedMilliseconds);

            });
            //using var dataTable = await _fileReader.ReadAsDataTableAsync(fullPath);
            //await _bulkInsertService.InsertAsync(file.FileName, dataTable);

            return fullPath;
        }
    }
}
