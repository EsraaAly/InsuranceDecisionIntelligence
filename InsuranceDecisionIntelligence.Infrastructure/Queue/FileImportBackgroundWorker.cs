using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Abstractions.File;
using InsuranceDecisionIntelligence.Application.Abstractions.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Queue
{
    public class FileImportBackgroundWorker : BackgroundService
    {
        private readonly IQueueService _taskQueue;
        private readonly IServiceProvider _serviceProvider;

        public FileImportBackgroundWorker(IQueueService taskQueue, IServiceProvider serviceProvider)
        {
            _taskQueue = taskQueue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var filePath = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var importer = scope.ServiceProvider.GetRequiredService<IDataReaderSqlBulkImporter>();
                    var readerService = scope.ServiceProvider.GetRequiredService<ITabularFileReader>();

                    var reader = await readerService.ReadAsDataReaderAsync(filePath);
                    await importer.ImportAsync(filePath, Path.GetFileName(filePath), reader);
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
