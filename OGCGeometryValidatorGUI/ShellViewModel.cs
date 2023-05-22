using Microsoft.Toolkit.Mvvm.ComponentModel;
using OGCGeometryValidatorGUI.Infrastructure.MVVM;
using OGCGeometryValidatorGUI.Models;
using Ookii.Dialogs.Wpf;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace OGCGeometryValidatorGUI
{
    public class ShellViewModel : ObservableObject
    {
        private CancellationTokenSource cts = new();
        private OGCGeometryValidatorModel OgcModel { get; }
        private MessagesModel MessageModel { get; }

        #region handle commands

        public ICommand ScanCommand => new DelegatingCommand(SelectPathExecute, SelectPathCanExecute);
        public ICommand RunCommand =>
            new DelegatingCommand(async _ => await RunExecuteAsync(null, new Progress<ProgressModel>()), RunCanExecute);
        public ICommand CancelCommand => new DelegatingCommand(CancelExecute);

        #endregion

        #region parameter

        private string _startPath;
        public string StartPath
        {
            get => _startPath;
            set => SetProperty(ref _startPath, value);
        }


        private string _messages;
        public string Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }


        private int _geodatafilesCounter;

        public int GeodatafilesCounter
        {
            get => _geodatafilesCounter;
            set => SetProperty(ref _geodatafilesCounter, value);
        }


        private string? _progressPercentage;
        public string? ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private int _progressPercentageInt;
        public int ProgressPercentageInt
        {
            get => _progressPercentageInt;
            set => SetProperty(ref _progressPercentageInt, value);
        }

        private string? _appInfo;
        public string? AppInfo
        {
            get => _appInfo;
            set => SetProperty(ref _appInfo, value);
        }

        private bool _isRunEnabled;
        public bool IsRunEnabled
        {
            get => _isRunEnabled;
            set => SetProperty(ref _isRunEnabled, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private Stopwatch _runStopwatch;

        #endregion


        public ShellViewModel(OGCGeometryValidatorModel model)
        {
            _startPath = String.Empty;
            _messages = String.Empty;
            _isRunEnabled = true;
            _isBusy = false;

            OgcModel = model;
            MessageModel = new MessagesModel();

            AppInfo = OgcModel.AppInfo.ToString();

            _runStopwatch = new Stopwatch();
        }

        private void SelectPathExecute(object obj)
        {
            var openFolderDialog = new VistaFolderBrowserDialog();
            openFolderDialog.Multiselect = false;
            openFolderDialog.Description = "Select path to start the scan..";

            if (openFolderDialog.ShowDialog() == true)
            {
                StartPath = openFolderDialog.SelectedPath;
            }

            GeodatafilesCounter = OgcModel.GetSupportedVectorDataFilesInDirectory(StartPath);
        }

        private bool SelectPathCanExecute(object obj)
        {
            return true;
        }

        private async Task RunExecuteAsync(object obj, IProgress<ProgressModel> progress)
        {
            IsRunEnabled = false;
            IsBusy = true;

            _runStopwatch =new Stopwatch();
            _runStopwatch.Start();


            Console.WriteLine("Elapsed={0}", _runStopwatch.Elapsed);

            ProgressPercentage = "0 %";
            ProgressPercentageInt = 0;

            ((Progress<ProgressModel>)progress).ProgressChanged += ReportProgress;

            await OgcModel.ValidateFilesAsnyc(progress, cts.Token);


            IsRunEnabled = true;
            IsBusy = false;
            _runStopwatch.Stop();
        }


        private void ReportProgress(object? sender, ProgressModel e)
        {
            ProgressPercentage = $"{e.Percentage} %";
            ProgressPercentageInt = e.Percentage;

            foreach (var msg in MessageModel.GetMessageLines(e))
            {
                Messages += msg;
            }

            Messages += $"in { (double) _runStopwatch.ElapsedMilliseconds / (double) 1000:F1} seconds {Environment.NewLine}";
        }

        private bool RunCanExecute(object obj)
        {
            return StartPath.Length > 1 && GeodatafilesCounter > 1;
        }


        private void CancelExecute(object obj)
        {
            throw new NotImplementedException();
        }

    }
}
