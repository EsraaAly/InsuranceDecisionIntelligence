using System.Data;

namespace InsuranceDecisionIntelligence.Application.Abstractions.File;

public interface ITabularFileReader
{
    Task<DataTable> ReadAsDataTableAsync(string filePath);
    Task<IDataReader> ReadAsDataReaderAsync(string filePath);
}
