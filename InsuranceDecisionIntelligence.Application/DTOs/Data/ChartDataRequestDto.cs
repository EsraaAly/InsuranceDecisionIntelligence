using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.DTOs.Data
{
    public class ChartDataRequestDto
    {
        public int fileId { get; set; }
        public string XColumn { get; set; }
        public string YColumn { get; set; }
        public string Aggregation { get; set; } // "Sum", "Average", "Count", "Max", "Min"
        public bool Top10Only { get; set; }
    }
}
