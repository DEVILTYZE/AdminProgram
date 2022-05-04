using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using AdminProgramHost.Annotations;
using CommandLib;

namespace AdminProgramHost
{
    public sealed class Host : INotifyPropertyChanged
    {
        private const int Port = 8001;
        private static readonly string MacTxtPath = Environment.SpecialFolder.UserProfile + "\\mac.txt";

        private string _name;
        private string _ipAddress;
        private string _macAddress;
        private Thread _thread;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }
        }
        
        public string MacAddress
        {
            get => _macAddress;
            set
            {
                _macAddress = value;
                OnPropertyChanged(nameof(MacAddress));
            }
        }

        public Host() => SetStartInfo();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetStartInfo()
        {
            Name = Dns.GetHostName();

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);

            if (socket.LocalEndPoint is IPEndPoint endPoint)
                IpAddress = endPoint.Address.ToString();

            MacAddress = GetMacAddress();
            
            if (!File.Exists(MacTxtPath))
            {
                var client = new UdpClient(Port);

                try
                {
                    var mac = ReceiveData(client);

                    using var sw = new StreamWriter(MacTxtPath, false);
                    sw.Write(mac);
                }
                finally
                {
                    client.Close();
                }
            }

            if (_thread.IsAlive)
                _thread.Join();
            
            _thread = new Thread(OpenClient);
            _thread.Start();
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
                while (true)
                {
                    string data;
                
                    do
                    { 
                        data = ReceiveData(client);
                    } 
                    while (!IsRightServer(data));

                    data = ReceiveData(client);
                    var result = CommandAnalyzer.ExecuteCommand(int.Parse(data), MacAddress);
                    var resultBytes = result.ToBytes();
                    
                    client.Send(resultBytes, resultBytes.Length);
                    
                    // TODO: Добавить шифрование...
                }
            }
            catch (Exception) { restart = true; }
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
        /// <param name="isVerifying">Параметр проверки. True — если проверка сервера. False — если получение данных</param>
        /// <returns>Переданные данные в виде строки.</returns>
        private static string ReceiveData(UdpClient client)
        {
            IPEndPoint remoteIp = null;
            var data = client.Receive(ref remoteIp);
            var dataString = Encoding.Default.GetString(data);
            
            return dataString;
        }

        /// <summary>
        /// Метод нахождения верного сервера.
        /// </summary>
        /// <param name="data">MAC-адрес.</param>
        /// <returns>True или False.</returns>
        private static bool IsRightServer(string data)
        {
            using var sr = new StreamReader(MacTxtPath);
            var mac = sr.ReadToEnd();

            return string.CompareOrdinal(data, mac) == 0;
        }
    }
}