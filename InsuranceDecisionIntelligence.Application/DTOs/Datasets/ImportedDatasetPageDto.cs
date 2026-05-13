namespace InsuranceDecisionIntelligence.Application.DTOs.Datasets;

public class ImportedDatasetPageDto
{
    public dynamic Data { get; set; } = null!;
    public int Count { get; set; }
    public long RowsCount { get; set; }
    public int ColumnsCount { get; set; }
    public DateTime UploadedDate { get; set; }
}
