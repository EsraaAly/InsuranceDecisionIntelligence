using InsuranceDecisionIntelligence.Application.Abstractions.Data;
using InsuranceDecisionIntelligence.Application.Abstractions.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.PersistentJobQueue
{
    public class FileImportJobProcessor
    {
        private readonly ITabularFileReader _reader;
        private readonly IDataReaderSqlBulkImporter _importer;

        public FileImportJobProcessor(ITabularFileReader reader, IDataReaderSqlBulkImporter importer)
        {
            _reader = reader;
            _importer = importer;
        }

        public async Task ProcessFileAsync(string filePath, string fileName)
        {
            var dataReader = await _reader.ReadAsDataReaderAsync(filePath);
            await _importer.ImportAsync(filePath, fileName, dataReader);
        }
    }
}
