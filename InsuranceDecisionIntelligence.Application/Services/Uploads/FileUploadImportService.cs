using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Abstractions.File;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InsuranceDecisionIntelligence.Application.Services.Uploads;

public class FileUploadImportService : IFileUploadImportService
{
    private readonly IFileStorageService _fileStorage;
    private readonly ITabularFileReader _tabularFileReader;
    private readonly IDataReaderSqlBulkImporter _dataReaderBulkImporter;
    private readonly ILogger<FileUploadImportService> _logger;

    public FileUploadImportService(
        IFileStorageService fileStorage,
        ITabularFileReader tabularFileReader,
        IDataReaderSqlBulkImporter dataReaderBulkImporter,
        ILogger<FileUploadImportService> logger)
    {
        _fileStorage = fileStorage;
        _tabularFileReader = tabularFileReader;
        _dataReaderBulkImporter = dataReaderBulkImporter;
        _logger = logger;
    }

    public async Task<string> SaveAndQueueImportAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
        var folderPath = Path.Combine(
            basePath,
            "InsuranceDecisionIntelligence.Infrastructure",
            "FileStorage",
            "Uploads");

        await _fileStorage.CreateDirectoryAsync(folderPath);
        string fullPath = await _fileStorage.SaveAsync(file, folderPath);

        _ = Task.Run(async () =>
        {
            var reader = await _tabularFileReader.ReadAsDataReaderAsync(fullPath);
            await _dataReaderBulkImporter.ImportAsync(fullPath, file.FileName, reader);
        });

        return fullPath;
    }
}
