using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.DTOs.Data
{
    public class GetFileDetails
    {
        public dynamic Data { get; set; }
        public int Count { get; set; }
        public int RowsCount { get; set; }
        public int ColumnsCount { get; set; }
        public DateTime UploadedDate { get; set; }
    }
}
