using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using AdminProgram.Annotations;
using AdminProgram.Models;

namespace AdminProgram.ViewModels
{
    public sealed class LogViewModel : INotifyPropertyChanged
    {
        private static readonly string LogsDirPath = Environment.CurrentDirectory + "\\logs\\";
        
        private LogEntry _selectedLog;
        private bool _isWorkingWithFiles;

        public object Locker { get; }
        public ObservableCollection<LogEntry> Logs { get; }

        public LogEntry SelectedLog
        {
            get => _selectedLog;
            set
            {
                _selectedLog = value;
                OnPropertyChanged(nameof(SelectedLog));
            }
        }

        public bool IsWorkingWithFiles
        {
            get => _isWorkingWithFiles;
            set
            {
                _isWorkingWithFiles = value;
                OnPropertyChanged(nameof(IsWorkingWithFiles));
            }
        }

        public LogViewModel()
        {
            IsWorkingWithFiles = false;
            Locker = new object();
            Logs = new ObservableCollection<LogEntry>();
        }

        public void AddLog(string text, LogStatus status) => Logs.Add(new LogEntry(Logs.Count + 1, text, status));

        public bool ExportLogs()
        {
            if (Logs.Count == 0)
                return false;
            
            IsWorkingWithFiles = true;
            var thread = new Thread(ExportLogs);
            thread.Start(Logs.ToList());

            return true;
        }

        private void ExportLogs(object obj)
        {
            if (obj is null)
                return;

            if (!Directory.Exists(LogsDirPath))
                Directory.CreateDirectory(LogsDirPath);

            var logs = (List<LogEntry>)obj;
            var logName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".apl";
            using var sw = new StreamWriter(LogsDirPath + logName);
            
            foreach (var logEntry in logs)
                sw.WriteLine(logEntry);

            IsWorkingWithFiles = false;
        }

        public bool ImportLogs(string path)
        {
            if (!File.Exists(path))
                return false;

            IsWorkingWithFiles = true;
            var thread = new Thread(ImportLogs);
            thread.Start(path);

            return true;
        }

        private void ImportLogs(object obj)
        {
            if (obj is null)
                return;

            var path = (string)obj;
            string[] logTexts;

            using (var sr = new StreamReader(path))
            {
                logTexts = sr.ReadToEnd().Split("/==/\r\n", StringSplitOptions.RemoveEmptyEntries);
            }

            Application.Current.Dispatcher.Invoke(ClearLogs);
            
            foreach (var logText in logTexts)
            {
                var data = logText.Split("/=/", StringSplitOptions.RemoveEmptyEntries);
                var log = new LogEntry(int.Parse(data[0]), Encoding.UTF8.GetString(data[3]
                        .Split(' ').Select(byte.Parse).ToArray()), (LogStatus)int.Parse(data[2]))
                    { CreationTime = DateTime.FromBinary(long.Parse(data[1])) };

                Application.Current.Dispatcher.Invoke(() => Logs.Add(log));
            }

            IsWorkingWithFiles = false;
        }

        public void ClearLogs() => Logs.Clear();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}