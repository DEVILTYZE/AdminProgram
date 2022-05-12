using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using CommandLib.Commands;
using Microsoft.Win32;

namespace AdminProgramHost
{
    public sealed partial class Host
    {
        public Host()
        {
            SetAutorunValue(true);
            SetStartInfo();
        }

        public void WaitThread() => _threadReceiveData.Join();

        private void SetStartInfo()
        {
            Name = Dns.GetHostName();

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
                IpAddress = endPoint.Address.ToString();

            MacAddress = GetMacAddress();
            
            // if (!File.Exists(MacDatPath))
            // {
            //     var client = new UdpClient(Port);
            //
            //     try
            //     {
            //         var mac = ReceiveData(client);
            //
            //         using var sw = new StreamWriter(MacDatPath, false);
            //         sw.Write(mac);
            //     }
            //     finally
            //     {
            //         client.Close();
            //     }
            // }

            if (_threadReceiveData.IsAlive)
                _threadReceiveData.Join();
            
            _threadReceiveData = new Thread(OpenClient);
            _threadReceiveData.Start();
        }

        private static string GetMacAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var adapter in interfaces)
            {
                var mac = adapter.GetPhysicalAddress().ToString();

                if (!string.IsNullOrEmpty(mac))
                    return mac;
            }

            throw new Exception("mac address don't exists");
        }

        /// <summary>
        /// Метод включения клиента в режим прослушки сети.
        /// </summary>
        private void OpenClient()
        {
            var client = new UdpClient(Port);
            var restart = false;

            try
            {
                var remoteIp = GetEndPoint();
                
                while (true)
                {
                    var data = ReceiveData(client, remoteIp);
                    // TODO: Изменить...
                    var command = new MessageCommand(null, new RSAParameters()); //AbstractCommand.FromBytes(Encoding.UTF8.GetBytes(data));

                    if (command is MessageCommand { IsSystem: true })
                        SetMacAddress(command.Execute());

                    var resultBytes = command.Execute().ToBytes();
                    client.Send(resultBytes, resultBytes.Length, remoteIp);

                    // TODO: Добавить шифрование...
                }
            }
            catch (Exception)
            {
                restart = true;
            }
            finally
            {
                client.Close();

                if (restart)
                {
                    Application.Current.Run();
                    Application.Current.Shutdown();
                }
            }
        }

        /// <summary>
        /// Метод получения данных по UDP клиенту.
        /// </summary>
        /// <param name="client">UDP-клиент.</param>
        /// <param name="remoteIp">Конечная точка принятия данных.</param>
        /// <returns>Переданные данные в виде строки.</returns>
        private static string ReceiveData(UdpClient client, IPEndPoint remoteIp)
        {
            var data = client.Receive(ref remoteIp);
            var dataString = Encoding.UTF8.GetString(data);
            
            return dataString;
        }

        private void SetMacAddress(CommandResult commandResult)
        {
            if (commandResult.Data is null || commandResult.Status == CommandResultStatus.Failed)
                return;

            _mainMacAddress = (string)commandResult.Data;

            using var sw = new StreamWriter(MacDatPath, false);
            sw.Write(_mainMacAddress);
        }

        private IPEndPoint GetEndPoint()
        {
            var arpProcess = new Process
            {
                StartInfo =
                {
                    FileName = "arp",
                    Arguments = "-a | find \"" + _mainMacAddress + '\"',
                    StandardOutputEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            var cmdOutput = arpProcess.StandardOutput.ReadToEnd();
            const string pattern = @"([1-2][0-5]*\.?){4}";

            var match = Regex.Match(cmdOutput, pattern);
            return new IPEndPoint(IPAddress.Parse(match.Value), Port);
        }

        private bool SetAutorunValue(bool isAutorun)
        {
            var path = Assembly.GetExecutingAssembly().Location;
            var reg = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run\");

            if (reg is null)
                return false;

            try
            {
                if (isAutorun)
                    reg.SetValue(Name + "Program", path);
                else
                    reg.DeleteValue(Name + "Program");
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}