using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using CommandLib;
using CommandLib.Commands;
using Microsoft.Win32;
using SecurityChannel;

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

            MacAddress = NetHelper.GetMacAddress();
            RsaEngine.GenerateKeys(out _privateKey, out _publicKey);

            if (_threadReceiveData.IsAlive)
                _threadReceiveData.Join();
            
            _threadReceiveData = new Thread(OpenClient);
            _threadReceiveData.Start();
        }

        /// <summary>
        /// Метод включения клиента в режим прослушки сети.
        /// </summary>
        private void OpenClient()
        {
            var client = new UdpClient();
            var restart = false;
            var remoteIp = GetEndPoint();

            try
            {
                while (true)
                {
                    // Шаг 1: принимаем данные.
                    var data = client.Receive(ref remoteIp);

                    // Шаг 2: декодируем данные в датаграмму.
                    var datagram = Datagram.FromBytes(data);

                    // Шаг 3: получаем из датаграммы команду.
                    var command = AbstractCommand.FromBytes(datagram.GetData(_privateKey), datagram.Type);

                    // Шаг 4: выполняем команду.
                    var result = command is MessageCommand { IsSystem: true }
                        ? SetMacAddress(command.Execute())
                        : command.Execute();

                    // Шаг 5: обновляем ключи.
                    RsaEngine.GenerateKeys(out _privateKey, out _publicKey);

                    // Шаг 6: добавляем новый публичный ключ к результату.
                    result.PublicKey = new RsaKey(_publicKey);

                    // Шаг 7: формируем новую датаграмму из результата.
                    var resultBytes = result.ToBytes();
                    var resultDatagram = new Datagram(resultBytes, AesEngine.GetKey(),
                        command.RsaPublicKey, typeof(CommandResult).FullName, datagram.IsEncrypted);
                    var resultDatagramBytes = resultDatagram.ToBytes();

                    // Шаг 8: отправляем новую датаграмму.
                    client.Send(resultDatagramBytes, resultDatagramBytes.Length, remoteIp);
                }
            }
            catch (SocketException)
            {
                restart = true;
            }
            catch (Exception)
            {
                restart = true;
                var result = new CommandResult(CommandResultStatus.Failed, string.Empty);
                var datagram = new Datagram(result.ToBytes(), null, new RSAParameters(), 
                    typeof(CommandResult).FullName, false);
                var datagramBytes = datagram.ToBytes();

                client.Send(datagramBytes, datagramBytes.Length, remoteIp);
            }
            finally
            {
                client.Close();

                if (restart)
                {
                    Process.Start(Application.ResourceAssembly.Location);
                    Application.Current.Shutdown();
                }
            }
        }

        private CommandResult SetMacAddress(CommandResult commandResult)
        {
            if (commandResult.Data is null || commandResult.Status == CommandResultStatus.Failed)
                return new CommandResult(CommandResultStatus.Failed, null);

            _mainMacAddress = (string)commandResult.Data;

            using var sw = new StreamWriter(MacDatPath, false);
            sw.Write(_mainMacAddress);

            return new CommandResult(CommandResultStatus.Successed, null);
        }

        private IPEndPoint GetEndPoint()
        {
            var arpProcess = new Process
            {
                StartInfo =
                {
                    FileName = "arp",
                    Arguments = "-a | find \"" + _mainMacAddress + '\"',
                    StandardOutputEncoding = Encoding.Unicode,
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