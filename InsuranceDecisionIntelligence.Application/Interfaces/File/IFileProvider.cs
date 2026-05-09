using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.File
{
    public interface IFileProvider
    {
        public Task<string> SaveAync(IFormFile file, string folderPath);
        public Task CreateDirectoryAsync(string folderPath);
        public Task<bool> DeleteAsync(string folderPath, string filename);
        public Task<byte[]> DownloadAsync(string Path, string filename);
        public Task<Stream> OpenAsync(string FolderPath, string filename);
    }
}
