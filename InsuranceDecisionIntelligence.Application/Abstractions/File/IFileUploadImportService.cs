using Microsoft.AspNetCore.Http;

namespace InsuranceDecisionIntelligence.Application.Abstractions.File;

public interface IFileUploadImportService
{
    Task<string> SaveAndQueueImportAsync(IFormFile file);
}
