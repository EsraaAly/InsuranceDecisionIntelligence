namespace InsuranceDecisionIntelligence.Application.DTOs.Datasets;

public class DatasetChartQueryRequest
{
    public int FileId { get; set; }
    public string XColumn { get; set; } = string.Empty;
    public string YColumn { get; set; } = string.Empty;
    public string Aggregation { get; set; } = string.Empty;
    public bool Top10Only { get; set; }
}
