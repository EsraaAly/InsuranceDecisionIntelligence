using InsuranceDecisionIntelligence.Application.DTOs.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.File;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace InsuranceDecisionIntelligence.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IDataQueryService _dataQueryService;


        public FileController(IFileService fileService,IDataQueryService dataQueryService)
        {
            _fileService = fileService;
            _dataQueryService = dataQueryService;
        }

        [HttpPost("Upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(long.MaxValue)]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadAsync([FromForm] UploadFileDto dto)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string filepath = await _fileService.ProcessFile(dto.File);
            stopwatch.Stop();
            return Ok(new
            {
                Data = filepath,
                TimeTaken = stopwatch.ElapsedMilliseconds
            });
        }

        [HttpGet("Read")]
        public async Task<IActionResult> ReadAsync([FromQuery] string filepath, [FromQuery] int page, [FromQuery] int pageSize)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _dataQueryService.ReadFileAsync(filepath, page, pageSize);
            stopwatch.Stop();
            return Ok(new
            {
                TimeTaken = stopwatch.ElapsedMilliseconds,
                DataCount = result.Count,
                Data = result.Data
            });
        }

        //[HttpGet("ultrafast")]
        //public async Task<IActionResult> ReadUltraFastAsync([FromQuery] string filepath, [FromQuery] int page, [FromQuery] int pageSize)
        //{
        //    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        //    var result = await _fileService.ReadFileUltraFastAsync(filepath, page, pageSize);
        //    stopwatch.Stop();
        //    return Ok(new
        //    {
        //        DataCount = result.Count,
        //        Data = result,
        //        TimeTaken = stopwatch.ElapsedMilliseconds
        //    });
        //}

        //[HttpGet("partial")]
        //public async Task<IActionResult> ReadPartialAsync([FromQuery] string filepath, [FromQuery] long startByte, [FromQuery] long endByte, [FromQuery] int page, [FromQuery] int pageSize)
        //{
        //    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        //    var result = await _fileService.ReadFilePartialAsync(filepath, startByte, endByte, page, pageSize);
        //    stopwatch.Stop();
        //    return Ok(new
        //    {
        //        DataCount = result.Count,
        //        Data = result,
        //        TimeTaken = stopwatch.ElapsedMilliseconds
        //    });
        //}

        //[HttpGet("firstpage")]
        //public async Task<IActionResult> ReadFirstPageAsync([FromQuery] string filepath)
        //{
        //    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        //    var result = await _fileService.ReadFirstPageAsync(filepath);
        //    stopwatch.Stop();
        //    return Ok(new
        //    {
        //        DataCount = result.Count,
        //        Data = result,
        //        TimeTaken = stopwatch.ElapsedMilliseconds
        //    });
        //}
    }
}
