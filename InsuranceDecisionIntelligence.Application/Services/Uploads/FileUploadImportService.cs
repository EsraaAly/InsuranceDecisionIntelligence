using Hangfire;
using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Abstractions.File;
using InsuranceDecisionIntelligence.Application.Abstractions.Queue;
using InsuranceDecisionIntelligence.Application.Contracts.Event_Driven;
using InsuranceDecisionIntelligence.Application.PersistentJobQueue;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace InsuranceDecisionIntelligence.Application.Services.Uploads;

public class FileUploadImportService : IFileUploadImportService
{
    private readonly IFileStorageService _fileStorage;
    private readonly ITabularFileReader _tabularFileReader;
    private readonly IDataReaderSqlBulkImporter _dataReaderBulkImporter;
    private readonly ILogger<FileUploadImportService> _logger;
    //private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    //private readonly IQueueService _taskQueue;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IPublishEndpoint _publishEndpoint;


    public FileUploadImportService(
        IFileStorageService fileStorage,
        ITabularFileReader tabularFileReader,
        IDataReaderSqlBulkImporter dataReaderBulkImporter,
        //IQueueService taskQueue,
        IBackgroundJobClient backgroundJobClient,
        IPublishEndpoint publishEndpoint,
        ILogger<FileUploadImportService> logger)
    {
        _fileStorage = fileStorage;
        _tabularFileReader = tabularFileReader;
        _dataReaderBulkImporter = dataReaderBulkImporter;
        //_taskQueue = taskQueue;
        _backgroundJobClient = backgroundJobClient;
        _publishEndpoint = publishEndpoint;
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

        await _publishEndpoint.Publish<FileUploadedEvent>(new FileUploadedEvent(fullPath, file.FileName));

        //_backgroundJobClient.Enqueue<FileImportJobProcessor>(processor =>
        //    processor.ProcessFileAsync(fullPath, file.FileName));

        //await _taskQueue.QueueBackgroundWorkItemAsync(fullPath);

        //_ = Task.Run(async () =>
        //{
        //    await _semaphore.WaitAsync();

        //    try
        //    {
        //        var sw = Stopwatch.StartNew();
        //        var startMemory = GC.GetTotalMemory(true) / 1024 / 1024;

        //        var reader = await _tabularFileReader.ReadAsDataReaderAsync(fullPath);
        //        Console.WriteLine($"[File: {file.FileName}] Read Done in {sw.ElapsedMilliseconds}ms | Memory used: {(GC.GetTotalMemory(false) / 1024 / 1024) - startMemory} MB");

        //        var before = GC.GetTotalAllocatedBytes();
        //        await _dataReaderBulkImporter.ImportAsync(fullPath, file.FileName, reader);
        //        var after = GC.GetTotalAllocatedBytes();

        //        Console.WriteLine($"[File: {file.FileName}] Insert Done at {sw.ElapsedMilliseconds}ms");
        //        _logger.LogInformation($"Allocated: {(after - before) / 1024 / 1024} MB");
        //        _logger.LogInformation($"GC Gen 0: {GC.CollectionCount(0)}, Gen 1: {GC.CollectionCount(1)}, Gen 2: {GC.CollectionCount(2)}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error occurred while processing file: {file.FileName}");
        //    }
        //    finally
        //    {
        //        _semaphore.Release();
        //    }
        //});

        return fullPath;
    }

}
