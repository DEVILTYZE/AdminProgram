using System.Collections.Generic;
using System.Threading;

namespace AdminProgram.Models
{
    public class ThreadList
    {
        private readonly List<Thread> _list;

        public ThreadList()
        {
            _list = new List<Thread>();
        }
        public void Add(Thread thread) => _list.Add(thread);

        public void WaitThreads()
        {
            foreach (var thread in _list)
                thread.Join();

            _list.Clear();
        }
    }
}