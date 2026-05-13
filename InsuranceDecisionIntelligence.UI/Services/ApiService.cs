using InsuranceDecisionIntelligence.UI.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.UI.Services
{
    public class ApiService : IDisposable
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public async Task<Result<List<UploadedFile>>> GetFilesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/File/files");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Result.Failure<List<UploadedFile>>(UIError.ApiError($"Failed to get files: {response.StatusCode}", errorContent));
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeAnonymousType(content, new { Success = false, Data = new List<UploadedFile>() });
                
                if (apiResponse?.Success != true)
                {
                    return Result.Failure<List<UploadedFile>>(UIError.ApiError("API returned unsuccessful response", content));
                }

                return Result.Success(apiResponse.Data!);
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure<List<UploadedFile>>(UIError.NetworkError("Network error occurred while getting files", ex.Message));
            }
            catch (Exception ex)
            {
                return Result.Failure<List<UploadedFile>>(UIError.InternalError("Unexpected error occurred while getting files", ex.Message));
            }
        }

        public async Task<Result<DataPreviewItem>> GetFilePreviewAsync(int id, int pageNo, int pageSize)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/File/preview?id={id}&pageNo={pageNo}&pageSize={pageSize}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Result.Failure<DataPreviewItem>(UIError.ApiError($"Failed to get file preview: {response.StatusCode}", errorContent));
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeAnonymousType(content, new { Success = false, Data = new DataPreviewItem() });
                
                if (apiResponse?.Success != true)
                {
                    return Result.Failure<DataPreviewItem>(UIError.ApiError("API returned unsuccessful response", content));
                }

                return Result.Success(apiResponse.Data);
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure<DataPreviewItem>(UIError.NetworkError("Network error occurred while getting file preview", ex.Message));
            }
            catch (Exception ex)
            {
                return Result.Failure<DataPreviewItem>(UIError.InternalError("Unexpected error occurred while getting file preview", ex.Message));
            }
        }

        public async Task<Result<UploadResponse>> UploadFileAsync(string filePath)
        {
            try
            {
                using var formContent = new MultipartFormDataContent();
                using var fileStream = System.IO.File.OpenRead(filePath);
                using var fileContent = new StreamContent(fileStream);

                formContent.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync("api/File/upload", formContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Result.Failure<UploadResponse>(UIError.ApiError($"Failed to upload file: {response.StatusCode}", errorContent));
                }

                var content = await response.Content.ReadAsStringAsync();
                var uploadResponse = JsonConvert.DeserializeObject<UploadResponse>(content);
                
                return Result.Success(uploadResponse!);
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure<UploadResponse>(UIError.NetworkError("Network error occurred while uploading file", ex.Message));
            }
            catch (System.IO.IOException ex)
            {
                return Result.Failure<UploadResponse>(UIError.FileError("File access error", ex.Message));
            }
            catch (Exception ex)
            {
                return Result.Failure<UploadResponse>(UIError.InternalError("Unexpected error occurred while uploading file", ex.Message));
            }
        }

        public async Task<Result<ChartResponse>> GetChartDataAsync(ChartDataRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/File/analyze/metadata", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Result.Failure<ChartResponse>(UIError.ApiError($"Failed to get chart data: {response.StatusCode}", errorContent));
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeAnonymousType(content, new { Success = false, Data = new List<dynamic>() });
                
                if (apiResponse?.Success != true)
                {
                    return Result.Failure<ChartResponse>(UIError.ApiError("API returned unsuccessful response", content));
                }

                // Convert dynamic metadata to ChartResponse format
                var chartResponse = new ChartResponse
                {
                    ChartTitle = $"Chart for {request.XColumn} vs {request.YColumn}",
                    XLabel = request.XColumn,
                    YLabel = request.YColumn,
                    DataPoints = ConvertMetadataToChartDataPoints(apiResponse.Data!)
                };
                
                return Result.Success(chartResponse);
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure<ChartResponse>(UIError.NetworkError("Network error occurred while getting chart data", ex.Message));
            }
            catch (Exception ex)
            {
                return Result.Failure<ChartResponse>(UIError.InternalError("Unexpected error occurred while getting chart data", ex.Message));
            }
        }

        private List<ChartDataPoint> ConvertMetadataToChartDataPoints(IEnumerable<dynamic> metadata)
        {
            var dataPoints = new List<ChartDataPoint>();
            
            foreach (var item in metadata)
            {
                try
                {
                    // SQL query returns items with Label and Value properties
                    string label = "Unknown";
                    double value = 1.0;
                    
                    // Case 1: Item is a dictionary/object with properties
                    if (item is IDictionary<string, object> dict)
                    {
                        // Extract Label and Value from SQL result
                        if (dict.TryGetValue("Label", out var lbl) && lbl != null)
                        {
                            label = lbl.ToString();
                        }
                        
                        if (dict.TryGetValue("Value", out var val) && val != null)
                        {
                            if (double.TryParse(val.ToString(), out double numValue))
                            {
                                value = numValue;
                            }
                        }
                    }
                    // Case 2: Item is a simple value (string, number)
                    else if (item is ValueType || item is string)
                    {
                        value = double.TryParse(item.ToString(), out double parsed) ? parsed : 1.0;
                        label = item.ToString();
                    }
                    // Case 3: Item is a complex object - try to get properties
                    else
                    {
                        var properties = item?.GetType().GetProperties();
                        if (properties != null)
                        {
                            foreach (var prop in properties)
                            {
                                try
                                {
                                    var propValue = prop.GetValue(item);
                                    if (propValue != null)
                                    {
                                        if (prop.Name.Equals("Label", StringComparison.OrdinalIgnoreCase))
                                        {
                                            label = propValue.ToString();
                                        }
                                        else if (prop.Name.Equals("Value", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (double.TryParse(propValue.ToString(), out double numValue))
                                            {
                                                value = numValue;
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    // Skip properties that can't be accessed
                                    continue;
                                }
                            }
                        }
                    }
                    
                    dataPoints.Add(new ChartDataPoint { Label = label, Value = value });
                }
                catch
                {
                    // Skip items that can't be processed
                    continue;
                }
            }
            
            return dataPoints;
        }
    }

    public class UploadResponse
    {
        public string Data { get; set; }
        public long TimeTaken { get; set; }
    }
}
