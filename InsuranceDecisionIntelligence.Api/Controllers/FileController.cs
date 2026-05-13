using InsuranceDecisionIntelligence.Api.Common;
using InsuranceDecisionIntelligence.Application.Common;
using InsuranceDecisionIntelligence.Application.DTOs.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.File;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IDataQueryService _dataQueryService;
        private readonly IMetadataAnalysisService _metadataAnalysisService;
        public FileController(IFileService fileService, IDataQueryService dataQueryService, IMetadataAnalysisService metadataAnalysisService)
        {
            _fileService = fileService;
            _dataQueryService = dataQueryService;
            _metadataAnalysisService = metadataAnalysisService;
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetFiles()
        {
            var result = await _dataQueryService.GetAllFilesAsync();
            return result.ToHttpResponse();
        }

        [HttpGet("preview")]
        public async Task<IActionResult> GetPreview([FromQuery] int id, [FromQuery] int pageNo, [FromQuery] int pageSize)
        {
            var result = await _dataQueryService.GetFileDetailsByIdAsync(id, pageNo, pageSize);
            return result.ToHttpResponse();
        }

        [HttpGet("jobs")]
        public IActionResult GetJobs()
        {
            var jobs = new[]
            {
                new { FileName = "sales_2024_05.csv", Status = "Processing", Progress = 75, StartedAt = "10:15 AM" },
                new { FileName = "orders_may.csv", Status = "Queued", Progress = 0, StartedAt = "10:20 AM" }
            };
            return Ok(jobs);
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(long.MaxValue)]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadAsync([FromForm] UploadFileDto dto)
        {
            var stopwatch = Stopwatch.StartNew();
            string filepath = await _fileService.ProcessFile(dto.File);
            stopwatch.Stop();
            
            return Ok(new
            {
                Data = filepath,
                TimeTaken = stopwatch.ElapsedMilliseconds
            });
        }

        [HttpPost("analyze/metadata")]
        public async Task<IActionResult> AnalyzeMetadataAsync([FromBody] ChartDataRequestDto request)
        {
            var result = await _metadataAnalysisService.AnalyzeMetadataAsync(request);
            return result.ToHttpResponse();
        }
    }
}
