namespace InsuranceDecisionIntelligence.Application.Configuration;

public class FileStorageOptions
{
    public string ServerType { get; set; } = "Local";
    public string FolderPath { get; set; } = string.Empty;
    public string FTPUserName { get; set; } = string.Empty;
    public string FTPPassword { get; set; } = string.Empty;
}
