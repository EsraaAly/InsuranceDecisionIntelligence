using Microsoft.AspNetCore.Http;

namespace InsuranceDecisionIntelligence.Application.Abstractions.File;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string folderPath);
    Task CreateDirectoryAsync(string folderPath);
    Task<bool> DeleteAsync(string folderPath, string filename);
    Task<byte[]> DownloadAsync(string folderPath, string filename);
    Task<Stream?> OpenReadAsync(string folderPath, string filename);
}
