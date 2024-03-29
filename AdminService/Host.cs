﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommandLib;
using CommandLib.Commands;
using CommandLib.Commands.Helpers;
using CommandLib.Commands.RemoteCommandItems;
using CommandLib.Commands.TransferCommandItems;
using SecurityChannel;

namespace AdminService
{
    public sealed partial class Host
    {
        public Host()
        {
            NetHelper.AddFirewallRules("AdminService", "TCP", true, true);
            NetHelper.AddFirewallRules("AdminService", "UDP", true, true);
            _savedCommands = new List<ICommand>();

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
                _ipAddress = endPoint.Address.ToString();
        }

        public void StartClientSession()
        {
            AreRunningTasks = true;
            _forceClose = false;

            _servers = new List<TcpListener>();
            Task.Run(() => OpenClient(NetHelper.CommandPort));
            Task.Run(() => OpenClient(NetHelper.TransferCommandPort));
            Task.Run(() => OpenClient(NetHelper.RemoteCommandPort));
        }

        public void WaitTasks()
        {
            if (_forceClose)
                return;
            
            _forceClose = true;
            
            if (_servers is not null)
                foreach(var server in _servers)
                    server.Stop();

            foreach (var command in _savedCommands)
                command.Abort();
        }

        /// <summary>
        /// Метод включения клиента в режим прослушки сети.
        /// </summary>
        private void OpenClient(object obj)
        {
            var port = (int)obj;
            var endPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), port);
            TcpListener server = null;
            RsaEngine.GenerateKeys(out var privateKey, out var publicKey);
            
            try
            {
                server = new TcpListener(endPoint);
                
                lock (_locker)
                    _servers.Add(server);
                
                server.Start();
                
                while (!_forceClose)
                {
                    var client = server.AcceptTcpClient();

                    // Шаг 1: принимаем данные.
                    using var stream = client.GetStream();
                    var data = NetHelper.StreamRead(stream);
                    
                    // Шаг 2: декодируем данные в датаграмму.
                    var datagram = Datagram.FromBytes(data);

                    // Шаг 3: получаем из датаграммы команду.
                    var command = AbstractCommand.FromBytes(datagram.GetData(privateKey), datagram.Type);

                    // Шаг 4: выполняем команду.
                    var result = CommandProcessing(command);

                    // Шаг 5: обновляем ключи.
                    RsaEngine.GenerateKeys(out privateKey, out publicKey);

                    // Шаг 6: добавляем новый публичный ключ к результату.
                    result.PublicKey = new RsaKey(publicKey);

                    // Шаг 7: формируем новую датаграмму из результата.
                    var resultBytes = result.ToBytes();
                    var resultDatagram = new Datagram(resultBytes, typeof(CommandResult), command.RsaPublicKey);
                    var resultDatagramBytes = resultDatagram.ToBytes();

                    // Шаг 8: отправляем новую датаграмму.
                    stream.Write(resultDatagramBytes, 0, resultDatagramBytes.Length);
                }
            }
            catch (SocketException)
            {
                // ignored
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                if (!_forceClose)
                    server?.Stop();
            }

            AreRunningTasks = false;
        }

        private CommandResult CommandProcessing(ICommand command)
        {
            switch (command.Type)
            {
                case CommandType.Execute:
                    var result = command.Execute();
                    AddCommand(command);
                            
                    return result;
                case CommandType.Abort:
                default:
                    var savedCommand = _savedCommands.FirstOrDefault(thisCommand 
                        => thisCommand.GetType() == command.GetType());
                            
                    if (savedCommand is null)
                        return new CommandResult(CommandResultStatus.Failed, null);

                    savedCommand.Abort();
                    _savedCommands.Remove(savedCommand);
                    
                    return new CommandResult(CommandResultStatus.Successed, null);
            }
        }

        private void AddCommand(ICommand command)
        {
            var types = new[] { typeof(RemoteCommand), typeof(TransferCommand) };

            foreach (var type in types)
                if (command.GetType().IsEquivalentTo(type))
                {
                    for (var i = 0; i < _savedCommands.Count; ++i)
                        if (command.GetType().IsEquivalentTo(_savedCommands[i].GetType()))
                        {
                            _savedCommands[i].Abort();
                            _savedCommands[i] = command;
                            return;
                        }
                    
                    _savedCommands.Add(command);
                }
        }
    }
}