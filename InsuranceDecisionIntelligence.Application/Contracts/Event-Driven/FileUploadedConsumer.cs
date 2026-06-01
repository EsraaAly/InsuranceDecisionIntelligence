using Hangfire;
using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Abstractions.File;
using InsuranceDecisionIntelligence.Application.PersistentJobQueue;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Contracts.Event_Driven
{
    public class FileUploadedConsumer : IConsumer<FileUploadedEvent>
    {
        //private readonly ITabularFileReader _reader;
        //private readonly IDataReaderSqlBulkImporter _importer;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public FileUploadedConsumer(
            IBackgroundJobClient backgroundJobClient
            //ITabularFileReader reader, 
            //IDataReaderSqlBulkImporter importer
            )
        {
            _backgroundJobClient = backgroundJobClient;
            //_reader = reader;
            //_importer = importer;
        }

        public async Task Consume(ConsumeContext<FileUploadedEvent> context)
        {
            string filePath = context.Message.FilePath;
            string fileName = context.Message.FileName;

            _backgroundJobClient.Enqueue<FileImportJobProcessor>(processor =>
                processor.ProcessFileAsync(filePath, fileName));
            //var dataReader = await _reader.ReadAsDataReaderAsync(filePath);
            //await _importer.ImportAsync(filePath, fileName, dataReader);
        }

    }
}
