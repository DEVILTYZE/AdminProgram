using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using AdminProgram.Annotations;
using SharpCompress.Common;

namespace AdminProgram.Models
{
    public enum LogStatus
    {
        Info = 0,
        Error = 1,
        Send = 2,
        Receive = 3
    }
    
    public sealed class LogEntry : INotifyPropertyChanged
    {
        private readonly int _id;
        private readonly string _text;
        private readonly LogStatus _status;
        private readonly DateTime _creationTime;

        public int Id
        {
            get => _id;
            init
            {
                if (value < -1)
                    throw new ArchiveException("ID не может быть меньше единцы");
                
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Text
        {
            get => _text;
            init
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    throw new NullReferenceException("Текст пуст или null");

                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public LogStatus Status
        {
            get => _status;
            init
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public DateTime CreationTime
        {
            get => _creationTime;
            init
            {
                _creationTime = value;
                OnPropertyChanged(nameof(CreationTime));
            }
        }

        public LogEntry(int id, string text, LogStatus status)
        {
            Id = id;
            Text = text;
            Status = status;
            CreationTime = DateTime.Now;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(Id + "/=/");
            sb.Append(CreationTime.ToBinary() + "/=/");
            sb.Append((int)Status + "/=/");
            sb.Append(string.Join(" ", Encoding.UTF8.GetBytes(Text)) + "/==/");

            return sb.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}