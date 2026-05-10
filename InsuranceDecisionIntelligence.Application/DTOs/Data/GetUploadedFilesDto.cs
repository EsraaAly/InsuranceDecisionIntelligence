using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.DTOs.Data
{
    public class GetUploadedFilesDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string UploadedAt { get; set; } = string.Empty;
    }
}
