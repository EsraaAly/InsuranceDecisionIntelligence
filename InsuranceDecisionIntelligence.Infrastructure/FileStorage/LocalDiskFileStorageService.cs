using InsuranceDecisionIntelligence.Application.Abstractions.File;
using Microsoft.AspNetCore.Http;

namespace InsuranceDecisionIntelligence.Infrastructure.FileStorage;

public class LocalDiskFileStorageService : IFileStorageService
{
    public Task CreateDirectoryAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string folderPath, string filename)
    {
        var fullPath = Path.Combine(folderPath, filename);

        if (!File.Exists(fullPath))
            throw new InvalidOperationException("File doesn't exist");

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task<byte[]> DownloadAsync(string folderPath, string filename)
    {
        throw new NotImplementedException();
    }

    public Task<Stream?> OpenReadAsync(string folderPath, string filename)
    {
        var path = Path.Combine(folderPath, filename);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return Task.FromResult<Stream?>(stream);
    }

    public async Task<string> SaveAsync(IFormFile file, string folderPath)
    {
        var originalName = Path.GetFileNameWithoutExtension(file.FileName);

        var extension = Path.GetExtension(file.FileName);

        var fileName = $"{originalName}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 4)}{extension}";

        var fullPath = Path.Combine(folderPath, fileName);

        if (File.Exists(fullPath))
            throw new InvalidOperationException("File already exists");

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return fullPath;
    }
}
