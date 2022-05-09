using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http;

namespace MyTestApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        #region Timer
        private const int TimerIntervalInSeconds = 300;
        private CancellationTokenSource _cancelTokenSource;
        #endregion

        #region isEnabled
        private bool _isEnabled = true;
        public bool isEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged("isEnabled");
                }
            }
        }
        #endregion

        #region ListUrl
        private List<string> myListUrl = new() { "https://api.twelvedata.com/time_series?apikey=e9b3641f40494fdf998b1eb8848ab689&interval=1min&format=JSON&symbol=EUR/USD&outputsize=1",
            "https://api.twelvedata.com/time_series?apikey=e9b3641f40494fdf998b1eb8848ab689&interval=1min&format=JSON&symbol=USD/JPY&outputsize=1",
            "https://api.twelvedata.com/time_series?apikey=e9b3641f40494fdf998b1eb8848ab689&interval=1min&format=JSON&symbol=ETH/USD&outputsize=1",
            "https://api.twelvedata.com/time_series?apikey=e9b3641f40494fdf998b1eb8848ab689&interval=1min&format=JSON&symbol=JPM&outputsize=1",
            "https://api.twelvedata.com/time_series?apikey=e9b3641f40494fdf998b1eb8848ab689&interval=1min&format=JSON&symbol=SPX&outputsize=1"
        };
        #endregion

        #region Path
        public string path = "";
        #endregion

        #region ListJS
        private List<JObject> ListJS = new();
        #endregion

        #region TextPrev
        private string _TextPrev = "";
        public string TextPrev
        {
            get => _TextPrev;
            set
            {
                if (_TextPrev != value)
                {
                    _TextPrev = value;
                    OnPropertyChanged("TextPrev");
                }
            }
        }
        #endregion

        #region StartProcces
        public ReactiveCommand<Unit, Unit> StartProcces { get; private set; }
        public async Task _StartProcces()
        {
            await _StopProcces();
            
            if (path == "")
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    SaveFileDialog dial = new();
                    var filter = new FileDialogFilter
                    {
                        Name = "Excel",
                        Extensions = {
                                "xlsx"
                                }
                    };
                    dial.Filters.Add(filter);
                    var res = await dial.ShowAsync(desktop.MainWindow);
                    if (res != null)
                    {
                        if (res.Count() != 0)
                        {
                            path = res;
                            if (!path.Contains(".xlsx"))
                            {
                                path += ".xlsx";
                            }
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }
                            _cancelTokenSource = new CancellationTokenSource();
                            isEnabled = false;
                            GetDataAll(_cancelTokenSource.Token);
                        }
                    }
                    else
                    {
                        TextPrev = "Нужно выбрать имя файла!";
                    }
                }
            }
            else
            {
                _cancelTokenSource = new CancellationTokenSource();
                isEnabled = false;
                GetDataAll(_cancelTokenSource.Token);
            }
        }
        #endregion

        #region StopProcces
        public ReactiveCommand<Unit, Unit> StopProcces { get; private set; }
        public async Task _StopProcces()
        {
            _cancelTokenSource?.Cancel();
            _cancelTokenSource = null;
            TextPrev = "";
            isEnabled = true;
        }
        #endregion

        #region GetData
        public bool IsWorking => _cancelTokenSource != null;

        public async Task GetDataAll(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
    
                var nextTime = Task.Delay(TimeSpan.FromSeconds(TimerIntervalInSeconds));
                try
                {
                    await Task.WhenAll(nextTime, GetData());
                    
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private async Task GetData()
        {
            var cur_it = 1;
            TextPrev = $"{DateTime.Now} Waiting for next 5 min...";
            foreach (var _url in myListUrl)
            {
                TextPrev = $"{DateTime.Now} Parse item: {cur_it}";
                try
                {
                    var client = new HttpClient();
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(_url),
                    };
                    using (var response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        JObject bodyJS = JObject.Parse(body);
                        ListJS.Add(bodyJS);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                TextPrev = $"{DateTime.Now} Done...";
                cur_it++;
            }
            PrintExcel();
            await Task.Delay(TimeSpan.FromSeconds(TimerIntervalInSeconds));
        }
        #endregion

        #region Excel
        public void PrintExcel() 
        {
            try
            {
                if (path != null)
                {
                    using (ExcelPackage excelPackage = new ExcelPackage(new FileInfo(path)))
                    {
                        excelPackage.Workbook.Properties.Author = "TEST_APP";
                        excelPackage.Workbook.Properties.Title = "Data";
                        excelPackage.Workbook.Properties.Created = DateTime.Now;

                        ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Список всех форм");
                        worksheet.Cells[1, 1].Value = "Код";
                        worksheet.Cells[1, 2].Value = "Название";
                        worksheet.Cells[1, 3].Value = "Значение";
                        worksheet.Cells[1, 4].Value = "Изменение";
                        worksheet.Cells[1, 5].Value = "Время обновления";
                        var row = 2;
                        if (!path.Contains(".xlsx"))
                        {
                            path += ".xlsx";
                        }
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        foreach (var rep in ListJS)
                        {
                            double ch = 0;
                            var ls1 = rep["values"][0].ToList();
                            var ls2 = rep["meta"].ToList();
                            var st = Convert.ToDouble(ls1[1].ToString().Replace("\"", "").Split(": ")[1].Replace(".", ","));
                            var en = Convert.ToDouble(ls1[4].ToString().Replace("\"", "").Split(": ")[1].Replace(".", ","));
                            if (st > en)
                            {
                                ch = ((en - st) / st) * 100;
                            }
                            else
                            {
                                ch = ((en - st) / st) * 100;
                            }
                            worksheet.Cells[row, 1].Value = row;
                            worksheet.Cells[row, 2].Value = ls2[0].ToString().Replace("\"", "").Split(": ")[1];
                            worksheet.Cells[row, 3].Value = st;
                            worksheet.Cells[row, 4].Value = ch;
                            worksheet.Cells[row, 5].Value = ls1[0].ToString().Replace("\"", "").Split(":")[1];
                            row++;
                        }
                        excelPackage.Save();
                        ListJS.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion

        #region Constacture
        public MainWindowViewModel() 
        {
            Init();
        }
        #endregion

        #region Init
        public async Task Init() 
        {
            StartProcces = ReactiveCommand.CreateFromTask(_StartProcces);
            StopProcces = ReactiveCommand.CreateFromTask(_StopProcces);
        }
        #endregion

        #region INotifyPropertyChanged
        protected void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
