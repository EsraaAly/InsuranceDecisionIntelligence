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
using System.Drawing;
using System.Drawing.Printing;
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
        private readonly HttpClient _httpClient;
        private Random _random = new Random();

        public int CurrentPage;
        private int TotalPage;
        private string fileName;
        private int selectedFileId;
        private ObservableCollection<int> pageNumbers = new ObservableCollection<int>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Initialize HTTP client for API calls
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:44314/"); // Change to your API URL
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            // Load data from API (no static data)
            _ = LoadUploadedFilesFromApiAsync();
        }

        #region Collections

        public ObservableCollection<UploadedFile> UploadedFiles { get; set; } = new();
        public ObservableCollection<dynamic> DataPreviewItems { get; set; } = new();
        public ObservableCollection<BackgroundJob> BackgroundJobs { get; set; } = new();

        #endregion

        #region Chart Data

        public ISeries[] LineChartData { get; set; } = Array.Empty<ISeries>();
        public Axis[] LineChartXAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] LineChartYAxes { get; set; } = Array.Empty<Axis>();

        public ISeries[] BarChartData { get; set; } = Array.Empty<ISeries>();
        public Axis[] BarChartXAxes { get; set; } = Array.Empty<Axis>();
        public Axis[] BarChartYAxes { get; set; } = Array.Empty<Axis>();

        public ISeries[] PieChartData { get; set; } = Array.Empty<ISeries>();

        #endregion

        #region API Methods

        // Load uploaded files from API

        private async Task LoadUploadedFilesFromApiAsync()
        {
            UploadedFiles.Clear();

            var filesResponse = await _httpClient.GetFromJsonAsync<List<UploadedFile>>("api/File/files");
            if (filesResponse != null)
            {
                foreach (var file in filesResponse)
                {
                    UploadedFiles.Add(file);
                }
            }
        }

        // Load data preview from API
        private async Task PreviewFileDataFromApiAsync(string fileName,int id,int pageNo, int PageSize)
        {
            DataPreviewItems.Clear();

            var previewResponse = await _httpClient.GetFromJsonAsync<DataPreviewItem>($"api/File/preview?id={id}&pageNo={pageNo}&pageSize={PageSize}");
            if (previewResponse != null)
            {
                txtColumnsCount.Text = previewResponse.ColumnsCount.ToString();
                txtRowsCount.Text = previewResponse.RowsCount.ToString();
                txtImportedDate.Text = "Import Date: " + previewResponse.UploadedDate.ToString();
                txtStatus.Text = "Status: Imported";
                txtFileName.Text = $"File Details ({fileName})";
                TotalPage = (int.Parse(txtRowsCount.Text)/ PageSize);
                if (previewResponse.Data is not null)
                {
                    txtShowingRows.Text = $"Showing {((pageNo - 1) * PageSize)+1}-{PageSize* pageNo} of {previewResponse.RowsCount} rows";
                    DG_FileData.ItemsSource = null;
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(previewResponse.Data.ToString());
                    DG_FileData.ItemsSource = dt.DefaultView;
                }
                RefreshUI();
            }
        }
        private async Task LoadDataFromApiAsync()
        {
            try
            {
                BackgroundJobs.Clear();

                //// Load background jobs from API
                //var jobsResponse = await _httpClient.GetFromJsonAsync<List<BackgroundJob>>("api/File/jobs");
                //if (jobsResponse != null)
                //{
                //    foreach (var job in jobsResponse)
                //    {
                //        BackgroundJobs.Add(job);
                //    }
                //}

                //// Load chart data from API
                //await LoadChartDataFromApiAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"API Error: {ex.Message}\nPlease check if the API server is running.", "API Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Load minimal fallback data
                LoadFallbackData();
            }
        }

        private async Task LoadChartDataFromApiAsync()
        {
            try
            {
                // Load line chart data
                var lineChartResponse = await _httpClient.GetFromJsonAsync<ChartData>("api/File/charts/line");
                if (lineChartResponse != null)
                {
                    LineChartData = lineChartResponse.Series;
                    LineChartXAxes = lineChartResponse.XAxes;
                    LineChartYAxes = lineChartResponse.YAxes;
                }

                // Load bar chart data
                var barChartResponse = await _httpClient.GetFromJsonAsync<ChartData>("api/File/charts/bar");
                if (barChartResponse != null)
                {
                    BarChartData = barChartResponse.Series;
                    BarChartXAxes = barChartResponse.XAxes;
                    BarChartYAxes = barChartResponse.YAxes;
                }

                // Load pie chart data
                var pieChartResponse = await _httpClient.GetFromJsonAsync<PieChartDataResponse>("api/File/charts/pie");
                if (pieChartResponse != null)
                {
                    PieChartData = pieChartResponse.Series;
                }

                OnPropertyChanged(nameof(LineChartData));
                OnPropertyChanged(nameof(LineChartXAxes));
                OnPropertyChanged(nameof(LineChartYAxes));
                OnPropertyChanged(nameof(BarChartData));
                OnPropertyChanged(nameof(BarChartXAxes));
                OnPropertyChanged(nameof(BarChartYAxes));
                OnPropertyChanged(nameof(PieChartData));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chart data loading error: {ex.Message}", "API Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task UploadFileAsync(string filePath)
        {
            try
            {
                var formContent = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                formContent.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync("api/File/upload", formContent);
                
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("File uploaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    //await LoadUploadedFilesFromApiAsync(); // Refresh all data
                }
                else
                {
                    MessageBox.Show($"Upload failed: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Upload error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Event Handlers

        private async void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Select CSV File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await UploadFileAsync(openFileDialog.FileName);
            }
        }

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            // 2. Extract the data object from the CommandParameter
            var selectedRowData = button.CommandParameter as UploadedFile;
            if (selectedRowData != null)
            {
                int pageNo = 0;
                int pageSize = 0;
                if (txtPageNo.Text.Length == 0 || txtPageNo.Text == "0")
                {
                    txtPageNo.Text = "1";

                    pageNo = 1;
                }
                else
                {
                    pageNo = int.Parse(txtPageNo.Text);

                }
                pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());
                fileName = selectedRowData.FileName;
                selectedFileId = selectedRowData.Id;
                await PreviewFileDataFromApiAsync(fileName, selectedFileId, pageNo, pageSize);
                
            }
        }
        private async void txtPageNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            CurrentPage = int.Parse(txtPageNo.Text);
            //HighlightActivePage();
            //UpdatePaginationButtons();
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }


        //private void UpdatePaginationButtons()
        //{
        //    btnFirstPage.IsEnabled = (CurrentPage > 1);
        //    btnPreviousPage.IsEnabled = (CurrentPage > 1);
        //    btnPreviousButtons.IsEnabled = (CurrentPage > 1);

        //    btnLastPage.IsEnabled = (CurrentPage >= TotalPage);
        //    btnNextPage.IsEnabled = (CurrentPage >= TotalPage);
        //    btnNextButtons.IsEnabled = (CurrentPage >= TotalPage);
        //}
        //private void HighlightActivePage()
        //{
        //    Button[] pageButtons = { btnNum1, btnNum2, btnNum3, btnNum4 };
        //    foreach (var button in pageButtons)
        //    {
        //        if (button.Content.ToString() == CurrentPage.ToString())
        //        {
        //            button.Background = (System.Windows.Media.Brush) new BrushConverter().ConvertFrom("#FF2196F3");
        //            button.FontWeight = FontWeights.Bold;
        //        }
        //        else
        //        {
        //            button.Background = System.Windows.Media.Brushes.Transparent;
        //            button.FontWeight = FontWeights.Normal;
        //        }
        //    }
        //}

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadUploadedFilesFromApiAsync();
        }

        #endregion

        #region Fallback Data

        private void LoadFallbackData()
        {
            // Load minimal fallback data when API is not available
            //UploadedFiles.Add(new UploadedFile { FileName = "No API Connection", UploadedAt = "N/A"});
            
            //DataPreviewItems.Add(new DataPreviewItem
            //{
            //    Column1 = "N/A",
            //    Column2 = "N/A",
            //    Column3 = "N/A",
            //    Column4 = "N/A",
            //    Column5 = "N/A",
            //    Column30 = "N/A"
            //});
            
            //BackgroundJobs.Add(new BackgroundJob { FileName = "N/A", Status = "No API", Progress = 0, StartedAt = "N/A" });
            
            //// Load minimal chart data
            //LoadFallbackChartData();
        }

        private void HighlightCurrentPage()
        {
            // بنستنى الـ UI يرسم الزراير
            this.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in pagesControl.Items)
                {
                    // بنجيب الحاوية (Container) بتاعة الزرار
                    var container = pagesControl.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                    if (container != null)
                    {
                        var btn = VisualTreeHelper.GetChild(container, 0) as Button;
                        if (btn != null)
                        {
                            if (btn.Content.ToString() == CurrentPage.ToString())
                            {
                                btn.Background = (System.Windows.Media.Brush) new BrushConverter().ConvertFromString("#007ACC");
                                btn.Foreground = System.Windows.Media.Brushes.White;
                            }
                            else
                            {
                                btn.Background = System.Windows.Media.Brushes.White;
                                btn.Foreground = System.Windows.Media.Brushes.Black;
                            }
                        }
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private void LoadFallbackChartData()
        {
            // Minimal fallback chart data
            LineChartData = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new double[] { 0 },
                    Name = "No Data",
                    Stroke = new SolidColorPaint(SKColors.Gray),
                    Fill = null,
                    GeometrySize = 8
                }
            };

            LineChartXAxes = new Axis[] { new Axis { Labels = new[] { "No Data" } } };
            LineChartYAxes = new Axis[] { new Axis { MinLimit = 0 } };

            BarChartData = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = new double[] { 0 },
                    Name = "No Data",
                    Stroke = new SolidColorPaint(SKColors.Gray),
                    Fill = new SolidColorPaint(SKColors.Gray)
                }
            };

            BarChartXAxes = new Axis[] { new Axis { Labels = new[] { "No Data" } } };
            BarChartYAxes = new Axis[] { new Axis { MinLimit = 0 } };

            PieChartData = new ISeries[]
            {
                new PieSeries<double> { Values = new double[] { 100 }, Name = "No Data", Fill = new SolidColorPaint(SKColors.Gray) }
            };
        }

        #endregion

        #region Data Models for API

        public class ChartData
        {
            public ISeries[] Series { get; set; } = Array.Empty<ISeries>();
            public Axis[] XAxes { get; set; } = Array.Empty<Axis>();
            public Axis[] YAxes { get; set; } = Array.Empty<Axis>();
        }

        public class PieChartDataResponse
        {
            public ISeries[] Series { get; set; } = Array.Empty<ISeries>();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _httpClient?.Dispose();
            base.OnClosed(e);
        }

        private async void combPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int pageSize = int.Parse(((ComboBoxItem) combPageSize.SelectedItem).Content.ToString());
            txtPageNo.Text = "1";
            if (txtRowsCount.Text.Length == 0)
            {
                TotalPage = 0;
            }
            else
            {
                TotalPage = (int.Parse(txtRowsCount.Text) / pageSize);

            }

            if (fileName != null && selectedFileId != 0)
            {
                await PreviewFileDataFromApiAsync(fileName, selectedFileId, CurrentPage, pageSize);
            }
        }

        //private void btnPreviousButtons_Click(object sender, RoutedEventArgs e)
        //{
        //    Button[] pageButtons = { btnNum1, btnNum2, btnNum3, btnNum4 };
        //    // btnNum1, btnNum2, btnNum3, btnNum4
        //    //5        6          7       8
        //    //1         2           3       4

        //    if ((int) btnNum1.Content == 4)
        //    {

        //    }
        //    btnNum4.Content = btnNum1.Content;
        //    //
        //    foreach (var button in pageButtons)
        //    {
        //        if (button.Content.ToString() == CurrentPage.ToString())
        //        {

        //        }
        //        else
        //        {

        //        }
        //    }

        //    HighlightActivePage();
        //    UpdatePaginationButtons();
        //}

        private void btnNextButtons_Click(object sender, RoutedEventArgs e)
        {

        }
        private async void RefreshUI()
        {
            pageNumbers.Clear();

            btnFirst.IsEnabled = btnPrev.IsEnabled = (CurrentPage > 1);
            btnNext.IsEnabled = btnLast.IsEnabled = (CurrentPage < TotalPage);
            int maxButtons = 5;
            
            int start = Math.Max(1, CurrentPage - 2);
            int end = Math.Min(TotalPage, start + maxButtons - 1);

            if (end == TotalPage) start = Math.Max(1, end - 4);

            for (int i = start; i <= end; i++) pageNumbers.Add(i);

            pagesControl.ItemsSource = pageNumbers;

            int pageNo = 0;
            int pageSize = 0;
            if (txtPageNo.Text.Length == 0 || txtPageNo.Text == "0")
            {
                txtPageNo.Text = "1";

                pageNo = 1;
            }
            else
            {
                pageNo = int.Parse(txtPageNo.Text);

            }
            pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());

            HighlightCurrentPage();
        }
        private async void First_Click(object sender, RoutedEventArgs e) 
        { 
            CurrentPage = 1;
            int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());

            await PreviewFileDataFromApiAsync(fileName, selectedFileId, CurrentPage, pageSize);
            txtPageNo.Text = CurrentPage.ToString();
        }
        private async void Prev_Click(object sender, RoutedEventArgs e) 
        {
            CurrentPage--;
            int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());

            await PreviewFileDataFromApiAsync(fileName, selectedFileId, CurrentPage, pageSize);
            txtPageNo.Text = CurrentPage.ToString();
        }
        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage++;
            int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());

            await PreviewFileDataFromApiAsync(fileName, selectedFileId, CurrentPage, pageSize);
            txtPageNo.Text = CurrentPage.ToString(); 
        }
        private async void Last_Click(object sender, RoutedEventArgs e) 
        {
            CurrentPage = TotalPage;
            int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());

            await PreviewFileDataFromApiAsync(fileName, selectedFileId, CurrentPage, pageSize);
            txtPageNo.Text = CurrentPage.ToString();
        }

        private async void PageNumber_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if(CurrentPage!= (int)btn.Content)
            {
                CurrentPage = (int)btn.Content;
                int pageSize = int.Parse(((ComboBoxItem)combPageSize.SelectedItem).Content.ToString());
                await PreviewFileDataFromApiAsync(fileName, selectedFileId, CurrentPage, pageSize);
                txtPageNo.Text = CurrentPage.ToString();
            }       

        }

        private void Button_Loaded(object sender, RoutedEventArgs e)
        {

        }

        //private void UpdatePagination()
        //{
        //    pageNumbers.Clear();
        //    int maxButtons = 5;
        //    int start = Math.Max(1,CurrentPage - 1);
        //    int end = Math.Min(TotalPage, start + maxButtons - 1);

        //    if (end == TotalPage) start = Math.Max(1, end - maxButtons + 1);

        //    for (int i = start;i<=end;i++)
        //    {
        //        pageNumbers.Add(i);
        //    }
        //}

    }

    // Data Models
    public class UploadedFile
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string UploadedAt { get; set; } = string.Empty;
    }

    public class DataPreviewItem
    {
        public dynamic Data { get; set; }
        public int Count { get; set; }
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
}
