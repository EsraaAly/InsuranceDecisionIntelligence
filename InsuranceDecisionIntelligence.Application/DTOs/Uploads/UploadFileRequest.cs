using Microsoft.AspNetCore.Http;

namespace InsuranceDecisionIntelligence.Application.DTOs.Uploads;

public class UploadFileRequest
{
    public IFormFile File { get; set; } = null!;
}
