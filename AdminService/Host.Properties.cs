using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using CommandLib.Commands.Helpers;

namespace AdminService
{
    public partial class Host
    {
        private readonly object _locker = new();
        private readonly string _name, _ipAddress, _macAddress;
        private List<TcpListener> _servers;
        private bool _forceClose, _restart;
        private readonly List<ICommand> _savedCommands;

        private static string ProgramName
        {
            get
            {
                var directory = new DirectoryInfo(Environment.CurrentDirectory);
                return directory.GetFiles("*.exe").FirstOrDefault()?.FullName;
            }
        }

        public bool AreRunningTasks;
    }
}