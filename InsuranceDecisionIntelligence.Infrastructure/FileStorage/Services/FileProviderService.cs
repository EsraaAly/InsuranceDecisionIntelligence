using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.FileStorage.Services
{
    public class FileProviderService : IFileProvider
    {

        public async Task CreateDirectoryAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public async Task<bool> DeleteAsync(string folderPath, string filename)
        {
            var fullPath = Path.Combine(folderPath, filename);

            if (!File.Exists(fullPath))
                throw new Exception("File doesn't exist");

            File.Delete(fullPath);

            return true;
        }

        public Task<byte[]> DownloadAsync(string Path, string filename)
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> OpenAsync(string FolderPath, string filename)
        {
            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));

            var subFolderName = Path.Combine(
                basePath,
                "InsuranceDecisionIntelligence.Infrastructure",
                "FileStorage",
                "Uploads"
            );

            if (!Directory.Exists(subFolderName))
            {
                Directory.CreateDirectory(subFolderName);
            }

            var path = Path.Combine(FolderPath, subFolderName, filename);

            if (!File.Exists(path)) return null;

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        }

        public async Task<string> SaveAync(IFormFile file,string folderPath)
        {            
            var fileName = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(folderPath, fileName);

            if (File.Exists(fullPath))
            {
                throw new Exception("File already exists");
            }

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fullPath;
        }

    }
}
