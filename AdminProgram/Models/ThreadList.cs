using System;
using System.Collections.Generic;
using System.Threading;
using AdminProgram.Annotations;
using System.Windows;
using AdminProgram.ViewModels;

namespace AdminProgram.Models
{
    public class ThreadList
    {
        private readonly HostViewModel _model;
        private readonly List<Thread> _list;

        public ThreadList(HostViewModel model)
        {
            _model = model;
            _list = new List<Thread>();
        }
        public void Add(Thread thread) => _list.Add(thread);

        public void WaitAllThreads(bool isScan)
        {
            var thread = new Thread(WaitThreads);
            thread.Start(isScan);
        }
        
        private void WaitThreads([NotNull] object obj)
        {
            foreach (var thread in _list)
                thread.Join();

            _list.Clear();
            
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((bool)obj)
                    _model.IsScanButtonEnabled = true;
                else
                    _model.IsRefreshButtonEnabled = true;
            }));
        }
    }
}