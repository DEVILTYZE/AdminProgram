using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AdminProgram.Annotations;

namespace AdminProgram.Helpers
{
    public sealed class TaskList : INotifyPropertyChanged
    {
        private readonly List<Task> _list;

        private bool _isAlive;

        public int Count => _list.Count(task => task.Status == TaskStatus.Running);
        
        public bool IsAlive
        {
            get => _isAlive;
            set
            {
                _isAlive = value;
                OnPropertyChanged(nameof(IsAlive));
            }
        }

        public TaskList() => _list = new List<Task>();

        public void Add(Task task)
        {
            var index = _list.FindIndex(thisTask => thisTask.IsCompleted);
            IsAlive = true;

            if (index != -1)
                _list[index] = task;
            else
                _list.Add(task);
        }

        public void Wait() => Task.Run(WaitTasks);

        private void WaitTasks()
        {
            foreach (var task in _list)
                task.Wait();

            _list.Clear();
            IsAlive = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}