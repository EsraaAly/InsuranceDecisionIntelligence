using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Common.Models;

public class FileProviderSettings
{
    public string ServerType { get; set; } = "Local"; // "Cloud" or "Local"
    public string FolderPath { get; set; } = string.Empty;
    public string FTPUserName { get; set; } = string.Empty;
    public string FTPPassword { get; set; } = string.Empty;
}

