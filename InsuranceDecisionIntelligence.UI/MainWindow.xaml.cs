using InsuranceDecisionIntelligence.Application.DTOs.Datasets;
using InsuranceDecisionIntelligence.Application.DTOs.Uploads;
using InsuranceDecisionIntelligence.UI.Common;
using InsuranceDecisionIntelligence.UI.Http;
using InsuranceDecisionIntelligence.UI.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InsuranceDecisionIntelligence.UI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields

        private readonly BackendApiClient _apiClient;
        private readonly System.Timers.Timer _refreshTimer;
        private readonly ObservableCollection<int> _pageNumbers = new();

        private int _currentPage;
        private int _totalPage;
        private int _totalRows;
        private int _totalColumns;
        private string _fileName;
        private int _selectedFileId;
        private TableMetadataDto? _currentTableMetadata;
        private DataTable _dt;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7039/"),
                Timeout = TimeSpan.FromMinutes(5)
            };
            _apiClient = new BackendApiClient(httpClient);

            _ = LoadUploadedFilesFromApiAsync();
        }

        #endregion

        #region Properties

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(nameof(CurrentPage)); }
        }

        public int TotalPage
        {
            get => _totalPage;
            set { _totalPage = value; OnPropertyChanged(nameof(TotalPage)); }
        }

        #endregion

        #region Collections

        public ObservableCollection<UploadedFileSummaryDto> UploadedFiles { get; set; } = new();
        public ObservableCollection<BackgroundJob> BackgroundJobs { get; set; } = new();
        public ObservableCollection<ColumnMetadataDto> CategoricalColumns { get; set; } = new();
        public ObservableCollection<ColumnMetadataDto> NumericalColumns { get; set; } = new();

        #endregion

        #region File Methods

        private async Task LoadUploadedFilesFromApiAsync()
        {
            try
            {
                var result = await _apiClient.GetUploadedSummariesAsync();
                
                result.Match(
                    onSuccess: files =>
                    {
                        UploadedFiles.Clear();
                        foreach (var file in files)
                            UploadedFiles.Add(file);
                    },
                    onFailure: error => ErrorHandlingService.ShowError(error, "Failed to load files")
                );
            }
            catch (Exception ex)
            {
                ErrorHandlingService.ShowError(UIError.InternalError("Failed to load files", ex.Message), "Failed to load files");
            }
        }

        private async Task UploadFileAsync(string filePath)
        {
            try
            {
                var result = await _apiClient.UploadFileAsync(filePath);
                
                result.Match(
                    onSuccess: response =>
                    {
                        ErrorHandlingService.ShowSuccess("File uploaded successfully!");
                        _ = LoadUploadedFilesFromApiAsync();
                    },
                    onFailure: error => ErrorHandlingService.ShowError(error, "Upload Failed")
                );
            }
            catch (Exception ex)
            {
                ErrorHandlingService.ShowError(UIError.FileError("Upload failed", ex.Message), "Upload Error");
            }
        }

        private async Task PreviewFileDataFromApiAsync(string fileName, int id, int pageNo, int pageSize)
        {
            try
            {
                var result = await _apiClient.GetImportedDatasetPageAsync(id, pageNo, pageSize);
                
                result.Match(
                    onSuccess: preview =>
                    {
                        _totalRows = preview.RowsCount;
                        _totalColumns = preview.ColumnsCount;

                        txtColumnsCount.Text = $"Total Rows: {preview.ColumnsCount}";
                        txtRowsCount.Text = $"Total Columns: {preview.RowsCount}";
                        txtImportedDate.Text = $"Import Date: {preview.UploadedDate}";
                        txtStatus.Text = "Status: Imported";
                        txtFileName.Text = $"File Details ({fileName})";
                        TotalPage = preview.RowsCount / pageSize;

                        if (preview.Data is not null)
                        {
                            txtShowingRows.Text = $"Showing {((pageNo - 1) * pageSize) + 1}-{pageSize * pageNo} of {preview.RowsCount} rows";
                            DG_FileData.ItemsSource = null;

                            _dt = JsonConvert.DeserializeObject<DataTable>(preview.Data.ToString());

                            if (_dt.Columns.Contains("TableId"))
                                _dt.Columns["TableId"].ColumnName = "Row_No";

                            DG_FileData.ItemsSource = _dt.DefaultView;
                        }

                        RefreshPagination();
                        
                        //if (_dt.Rows.Count > 0)
                        //    _ = LoadTableMetadataAsync(_dt);
                    },
                    onFailure: error => ErrorHandlingService.ShowError(error, "Preview Error")
                );
            }
            catch (Exception ex)
            {
                ErrorHandlingService.ShowError(UIError.InternalError("Preview failed", ex.Message), "Preview Error");
            }
        }

        #endregion

        #region Metadata & Chart Methods

        private void LoadTableMetadataAsync(DataTable dt)
        {
            //try
            //{
            //    txtChartStatus.Text = "Loading column metadata...";

            //    CategoricalColumns.Clear();
            //    NumericalColumns.Clear();

            //    foreach (DataColumn col in dt.Columns)
            //    {
            //        if (col.ColumnName == "Row_No") continue;

            //        var metadata = new ColumnMetadataDto
            //        {
            //            ColumnName = col.ColumnName,
            //            DataType = col.DataType.Name,
            //            ColumnType = GetColumnType(col, dt),
            //            UniqueCount = dt.DefaultView.ToTable(true, col.ColumnName).Rows.Count,
            //            SampleValue = dt.Rows.Count > 0 ? dt.Rows[0][col] : null
            //        };

            //        if (metadata.ColumnType == ColumnType.Categorical)
            //            CategoricalColumns.Add(metadata);
            //        else if (metadata.ColumnType == ColumnType.Numerical)
            //            NumericalColumns.Add(metadata);
            //    }

            //    _currentTableMetadata = new TableMetadataDto
            //    {
            //        FileId = _selectedFileId,
            //        TotalRows = dt.Rows.Count,
            //        CategoricalColumns = CategoricalColumns.ToList(),
            //        NumericalColumns = NumericalColumns.ToList()
            //    };

            //    ComboBox_X.ItemsSource = CategoricalColumns;
            //    ComboBox_Y.ItemsSource = NumericalColumns;

            //    txtChartStatus.Text = $"Loaded {CategoricalColumns.Count} categorical and {NumericalColumns.Count} numerical columns. Select columns to visualize.";
            //    return Task.CompletedTask;
            //}
            //catch (Exception ex)
            //{
            //    txtChartStatus.Text = "Error loading metadata";
            //    ErrorHandlingService.ShowError(UIError.InternalError("Failed to load metadata", ex.Message), "Metadata Error");
            //    return Task.CompletedTask;
            //}
        }

        

        //private async Task AnalyzeMetadataAsync()
        //{
        //    if (_currentTableMetadata == null)
        //    {
        //        ErrorHandlingService.ShowError(UIError.ValidationError("No file selected for metadata analysis"), "Metadata Analysis Error");
        //        return;
        //    }

        //    try
        //    {
        //        ChartLoadingBar.Visibility = Visibility.Visible;
        //        txtChartStatus.Text = "Analyzing metadata...";

        //        var request = new DatasetChartQueryRequest
        //        {
        //            FileId = _currentTableMetadata.FileId,
        //            XColumn = ComboBox_X.SelectedValue?.ToString(),
        //            YColumn = ComboBox_Y.SelectedValue?.ToString(),
        //            Aggregation = (ComboBox_Aggregation.SelectedItem as ComboBoxItem)?.Content.ToString(),
        //            Top10Only = CheckBox_Top10.IsChecked == true
        //        };

        //        var result = await _apiClient.GetChartDataAsync(request);
                
        //        result.Match(
        //            onSuccess: chartData =>
        //            {
        //                if (chartData?.DataPoints == null) return;

        //                txtChartStatus.Text = $"Chart generated — {chartData.DataPoints.Count} data points.";
        //                txtDynamicChartTitle.Text = chartData.ChartTitle;

        //                var values = chartData.DataPoints.Select(d => d.Value).ToArray();
        //                var labels = chartData.DataPoints.Select(d => d.Label).ToArray();

        //                if (BtnPie.IsChecked == true)
        //                {
        //                    BindPieChart(chartData.DataPoints);
        //                }
        //                else
        //                {
        //                    BindCartesianChart(values, labels, chartData.XLabel, chartData.YLabel);
        //                }
        //            },
        //            onFailure: error => ErrorHandlingService.ShowError(error, "Metadata Analysis Error")
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        txtChartStatus.Text = "Error analyzing metadata";
        //        ErrorHandlingService.ShowError(UIError.InternalError("Metadata analysis failed", ex.Message), "Metadata Analysis Error");
        //    }
        //    finally
        //    {
        //        ChartLoadingBar.Visibility = Visibility.Collapsed;
        //    }
        //}

        //private void BindCartesianChart(double[] values, string[] labels, string xLabel, string yLabel)
        //{
        //    MainCartesianChart.Visibility = Visibility.Visible;
        //    MainPieChart.Visibility = Visibility.Collapsed;

        //    ISeries series = BtnLine.IsChecked == true
        //        ? new LineSeries<double> { Values = values, Name = yLabel }
        //        : new ColumnSeries<double> { Values = values, Name = yLabel };

        //    MainCartesianChart.Series = new[] { series };
        //    MainCartesianChart.XAxes = new[] { new Axis { Labels = labels, Name = xLabel } };
        //}

        //private void BindPieChart(List<ChartDataPoint> dataPoints)
        //{
        //    MainCartesianChart.Visibility = Visibility.Collapsed;
        //    MainPieChart.Visibility = Visibility.Visible;

        //    MainPieChart.Series = dataPoints.Select(d => new PieSeries<double>
        //    {
        //        Values = new[] { d.Value },
        //        Name = d.Label
        //    }).ToArray();
        //}



        #endregion

        #region Pagination

        private void RefreshPagination()
        {
            _pageNumbers.Clear();

            btnFirst.IsEnabled = btnPrev.IsEnabled = CurrentPage > 1;
            btnNext.IsEnabled = btnLast.IsEnabled = CurrentPage < TotalPage;

            int start = Math.Max(1, CurrentPage - 2);
            int end = Math.Min(TotalPage, start + 4);
            if (end == TotalPage) start = Math.Max(1, end - 4);

            for (int i = start; i <= end; i++) _pageNumbers.Add(i);

            pagesControl.ItemsSource = _pageNumbers;
            HighlightCurrentPage();
        }

       
        private void HighlightCurrentPage()
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in pagesControl.Items)
                {
                    var container = pagesControl.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                    if (container == null) continue;

                    var btn = VisualTreeHelper.GetChild(container, 0) as Button;
                    if (btn == null) continue;

                    bool isActive = btn.Content.ToString() == CurrentPage.ToString();
                    btn.Background = isActive
                        ? (Brush)new BrushConverter().ConvertFromString("#007ACC")
                        : Brushes.White;
                    btn.Foreground = isActive ? Brushes.White : Brushes.Black;
                }
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private static ColumnType GetColumnType(DataColumn col, DataTable dt)
        {
            // Get up to 50 non-null sample values
            var samples = dt.AsEnumerable()
                            .Select(r => r[col]?.ToString()?.Trim())
                            .Where(v => !string.IsNullOrEmpty(v))
                            .Take(50)
                            .ToList();

            if (samples.Count == 0) return ColumnType.Categorical;

            int total = samples.Count;

            // Check Boolean
            var boolValues = new HashSet<string> { "true", "false", "yes", "no", "0", "1" };
            if (samples.All(v => boolValues.Contains(v.ToLower())))
                return ColumnType.Boolean;

            // Check DateTime
            int dateCount = samples.Count(v => DateTime.TryParse(v, out _));
            if ((double)dateCount / total >= 0.8)
                return ColumnType.Date;

            // Check Numerical
            int numCount = samples.Count(v => double.TryParse(v, out _));
            if ((double)numCount / total >= 0.8)
                return ColumnType.Numerical;

            // Check Categorical (low unique values = categorical)
            int uniqueCount = samples.Distinct(StringComparer.OrdinalIgnoreCase).Count();
            if ((double)uniqueCount / total <= 0.5)
                return ColumnType.Categorical;

            // Otherwise it's free text
            return ColumnType.Text;
        }

        private int GetPageSize() =>
            int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());

        #endregion

        #region Event Handlers — File

        private async void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv", Title = "Select CSV File" };
            if (dialog.ShowDialog() == true)
                await UploadFileAsync(dialog.FileName);
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) =>
            await LoadUploadedFilesFromApiAsync();

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.CommandParameter is not UploadedFileSummaryDto file) return;

            if (string.IsNullOrEmpty(txtPageNo.Text) || txtPageNo.Text == "0")
                txtPageNo.Text = "1";

            _fileName = file.FileName;
            _selectedFileId = file.Id;

            await PreviewFileDataFromApiAsync(_fileName, _selectedFileId, CurrentPage, GetPageSize());
            //if (_dt.Rows.Count > 0)
            //        await LoadTableMetadataAsync(_dt);
        }

        private async void combPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int pageSize = GetPageSize();
            txtPageNo.Text = "1";
            TotalPage = _totalRows / pageSize;

            if (_fileName != null && _selectedFileId != 0)
            {
                await PreviewFileDataFromApiAsync(_fileName, _selectedFileId, CurrentPage, pageSize);
            }
        }

        private void txtPageNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtPageNo.Text, out int result))
                CurrentPage = result;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) =>
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);

        #endregion

        #region Event Handlers — Pagination

        private async void First_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());
            await PreviewFileDataFromApiAsync(_fileName, _selectedFileId, CurrentPage, pageSize);
            txtPageNo.Text = CurrentPage.ToString();
        }
        private async void Prev_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage--;
            int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());
            await PreviewFileDataFromApiAsync(_fileName, _selectedFileId, CurrentPage, pageSize);
            txtPageNo.Text = CurrentPage.ToString();
        }
        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage++;
            int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());
            await PreviewFileDataFromApiAsync(_fileName, _selectedFileId, CurrentPage, pageSize);
            txtPageNo.Text = CurrentPage.ToString();
        }
        private async void Last_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage = TotalPage;
            int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());
            await PreviewFileDataFromApiAsync(_fileName, _selectedFileId, CurrentPage, pageSize);
            txtPageNo.Text = CurrentPage.ToString();
        }

        private async void PageNumber_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || CurrentPage == (int)btn.Content) return;
            CurrentPage = (int)btn.Content;
            txtPageNo.Text = CurrentPage.ToString();
            await PreviewFileDataFromApiAsync(_fileName, _selectedFileId, CurrentPage, GetPageSize());
        }

        #endregion

        #region Event Handlers — Charts

        //private async void RefreshCharts_Click(object sender, RoutedEventArgs e)
        //{
        //    RefreshChartsBtn.IsEnabled = false;
        //    RefreshChartsBtn.Content = "Refreshing...";
        //    try { await AnalyzeMetadataAsync(); }
        //    finally
        //    {
        //        RefreshChartsBtn.IsEnabled = true;
        //        RefreshChartsBtn.Content = "Refresh Charts";
        //    }
        //}

        //private async void ChartType_Click(object sender, RoutedEventArgs e)
        //{
        //    await AnalyzeMetadataAsync();
        //}

        //private async void ComboBox_X_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    await AnalyzeMetadataAsync();
        //}

        //private async void ComboBox_Y_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    await AnalyzeMetadataAsync();
        //}

        //private async void ComboBox_Aggregation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    await AnalyzeMetadataAsync();
        //}

        //private async void CheckBox_Top10_Changed(object sender, RoutedEventArgs e)
        //{
        //    await AnalyzeMetadataAsync();
        //}

        //private async void AnalyzeMetadata_Click(object sender, RoutedEventArgs e)
        //{
        //    await AnalyzeMetadataAsync();
        //}

        #endregion

        #region Helpers

        // Note: ErrorHandlingService is now used for all error display

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Lifecycle

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _apiClient?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }

    #region Data Models

    public class DataPreviewItem
    {
        public dynamic Data { get; set; }
        public int RowsCount { get; set; }
        public int ColumnsCount { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    public class BackgroundJob
    {
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string StartedAt { get; set; } = string.Empty;
    }

    public class ColumnMetadataDto
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public ColumnType ColumnType { get; set; }
        public int UniqueCount { get; set; }
        public object? SampleValue { get; set; }
    }

    public class TableMetadataDto
    {
        public int FileId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public List<ColumnMetadataDto> CategoricalColumns { get; set; } = new();
        public List<ColumnMetadataDto> NumericalColumns { get; set; } = new();
    }

    public class ChartResponse
    {
        public string ChartTitle { get; set; } = string.Empty;
        public string XLabel { get; set; } = string.Empty;
        public string YLabel { get; set; } = string.Empty;
        public List<ChartDataPoint> DataPoints { get; set; } = new();
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public enum ColumnType { Categorical, Numerical, Text, Date, Boolean }

    #endregion
}

