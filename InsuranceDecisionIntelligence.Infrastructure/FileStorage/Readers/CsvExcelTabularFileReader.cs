using ExcelDataReader;
using InsuranceDecisionIntelligence.Application.Abstractions.File;
using Sylvan.Data.Csv;
using System.Data;

namespace InsuranceDecisionIntelligence.Infrastructure.FileStorage.Readers;

public class CsvExcelTabularFileReader : ITabularFileReader
{
    public Task<IDataReader> ReadAsDataReaderAsync(string filePath)
    {
        var options = new CsvDataReaderOptions { HasHeaders = true };
        var reader = CsvDataReader.Create(filePath, options);
        return Task.FromResult((IDataReader)reader);
    }

    private static FileStream OpenFileStream(string filePath)
    {
        return new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            1048576,
            FileOptions.SequentialScan);
    }

    public Task<DataTable> ReadAsDataTableAsync(string filePath)
    {
        using var stream = OpenFileStream(filePath);
        var reader = ExcelReaderFactory.CreateCsvReader(stream);

        var result = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        return Task.FromResult(result.Tables[0]);
    }
}
