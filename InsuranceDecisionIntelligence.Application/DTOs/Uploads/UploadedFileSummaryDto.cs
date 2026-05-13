namespace InsuranceDecisionIntelligence.Application.DTOs.Uploads;

public class UploadedFileSummaryDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string UploadedAt { get; set; } = string.Empty;
}
