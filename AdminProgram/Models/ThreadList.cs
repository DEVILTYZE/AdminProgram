using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using AdminProgram.Annotations;

namespace AdminProgram.Models
{
    public sealed class ThreadList : INotifyPropertyChanged
    {
        private readonly List<Thread> _list;

        private bool _isDead;

        public bool IsDead
        {
            get => _isDead;
            set
            {
                _isDead = value;
                OnPropertyChanged(nameof(IsDead));
            }
        }

        public ThreadList()
        {
            _list = new List<Thread>();
        }
        
        public void Add(Thread thread)
        {
            IsDead = false;
            _list.Add(thread);
        }

        public void WaitThreads()
        {
            foreach (var thread in _list)
                thread.Join();

            _list.Clear();
            IsDead = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}