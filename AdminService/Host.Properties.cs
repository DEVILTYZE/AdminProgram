using System.Collections.Generic;
using System.Net.Sockets;
using CommandLib.Commands.Helpers;

namespace AdminService
{
    public partial class Host
    {
        private readonly object _locker = new();
        private readonly string _ipAddress;
        private List<TcpListener> _servers;
        private bool _forceClose;
        private readonly List<ICommand> _savedCommands;

        public bool AreRunningTasks;
    }
}