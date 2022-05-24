using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using AdminProgram.Annotations;

namespace AdminProgram.Models
{
    public sealed class ThreadList : INotifyPropertyChanged
    {
        private readonly List<Thread> _list;

        private bool _isAlive;

        public int Count => _list.Count(thread => !thread.IsAlive);
        
        public bool IsAlive
        {
            get => _isAlive;
            set
            {
                _isAlive = value;
                OnPropertyChanged(nameof(IsAlive));
            }
        }

        public ThreadList() => _list = new List<Thread>();

        public void Add(Thread thread)
        {
            IsAlive = true;
            RemoveUselessThreads();
            _list.Add(thread);
        }

        public void WaitThreads()
        {
            foreach (var thread in _list)
                thread.Join();

            _list.Clear();
            IsAlive = false;
        }

        private void RemoveUselessThreads() => _list.RemoveAll(thread => !thread.IsAlive);

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}