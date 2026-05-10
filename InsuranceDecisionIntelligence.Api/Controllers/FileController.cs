using InsuranceDecisionIntelligence.Application.DTOs.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using InsuranceDecisionIntelligence.Application.Interfaces.File;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

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

        // GET /api/files
        [HttpGet("files")]
        public async Task<IActionResult> GetFiles()
        {
            var files = await _dataQueryService.GetAllFilesAsync();
            if (files.IsNullOrEmpty())
            {
                return NotFound();
            }
            return Ok(files);
        }

        // GET /api/preview
        [HttpGet("preview")]
        public async Task<IActionResult> GetPreview([FromQuery] int id, [FromQuery] int pageNo, [FromQuery] int pageSize)
        {
            var result = await _dataQueryService.GetFileDetailsByIdAsync(id,pageNo,pageSize);

            return Ok(result);
        }

        // GET /api/jobs
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

        // GET /api/charts/line
        [HttpGet("charts/line")]
        public IActionResult GetLineChart()
        {
            var lineChartData = new
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] { 15000, 22000, 18000, 25000, 30000, 28000, 35000, 32000, 38000, 42000, 45000, 50000 },
                        Name = "Rows",
                        Stroke = new SolidColorPaint(SKColors.Blue),
                        Fill = null,
                        GeometrySize = 8
                    }
                },
                XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
                        LabelsRotation = 0
                    }
                },
                YAxes = new Axis[]
                {
                    new Axis
                    {
                        LabelsRotation = 0,
                        MinLimit = 0
                    }
                }
            };
            return Ok(lineChartData);
        }

        // GET /api/charts/bar
        [HttpGet("charts/bar")]
        public IActionResult GetBarChart()
        {
            var barChartData = new
            {
                Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = new double[] { 8500, 7200, 6800, 5500, 4800, 4200, 3800, 3200, 2800, 2100 },
                        Name = "Count",
                        Stroke = new SolidColorPaint(SKColors.Orange),
                        Fill = new SolidColorPaint(SKColors.Orange)
                    }
                },
                XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = new[] { "USA", "UK", "Canada", "Germany", "France", "Italy", "Spain", "Japan", "Australia", "Brazil" },
                        LabelsRotation = -45
                    }
                },
                YAxes = new Axis[]
                {
                    new Axis
                    {
                        LabelsRotation = 0,
                        MinLimit = 0
                    }
                }
            };
            return Ok(barChartData);
        }

        // GET /api/charts/pie
        [HttpGet("charts/pie")]
        public IActionResult GetPieChart()
        {
            var pieChartData = new
            {
                Series = new ISeries[]
                {
                    new PieSeries<double> { Values = new double[] { 35 }, Name = "< $500", Fill = new SolidColorPaint(SKColors.Green) },
                    new PieSeries<double> { Values = new double[] { 25 }, Name = "$500 - $1,000", Fill = new SolidColorPaint(SKColors.Orange) },
                    new PieSeries<double> { Values = new double[] { 20 }, Name = "$1,000 - $2,000", Fill = new SolidColorPaint(SKColors.Red) },
                    new PieSeries<double> { Values = new double[] { 20 }, Name = "> $2,000", Fill = new SolidColorPaint(SKColors.Gray) }
                }
            };
            return Ok(pieChartData);
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
            var result = await _dataQueryService.ReadFileDataAsync(filepath, page, pageSize);
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
