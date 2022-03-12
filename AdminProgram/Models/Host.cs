﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using AdminProgram.Annotations;

namespace AdminProgram.Models
{
    public enum HostStatus
    {
        On,
        Off,
        Loading,
        Unknown
    }

    public sealed class Host : INotifyPropertyChanged
    {
        private readonly string _name;
        private readonly string _ipAddress;
        private readonly string _macAddress;
        private HostStatus _status;
        
        public string Name
        {
            get => _name;
            init
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(value, "Value is null");

                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        
        public string IpAddress
        {
            get => _ipAddress;
            init
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(value, "Value is null");

                if (value.Length is > 15 or < 7)
                    throw new ArgumentException("Invalid IP-address length", value);

                if (value.Any(symbol => symbol is not '.' && !char.IsDigit(symbol)))
                    throw new ArgumentException("Invalid symbols in IP-address", value);

                _ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }
        }
        
        public string MacAddress
        {
            get => _macAddress;
            init
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(value, "Value is null");

                if (value.Length is > 17 or < 11)
                    throw new ArgumentException("Invalid IP-address length", value);

                if (value.Any(symbol => symbol is not ':' && !char.IsLetterOrDigit(symbol)))
                    throw new ArgumentException("Invalid symbols in IP-address", value);

                _macAddress = value;
                OnPropertyChanged(nameof(MacAddress));
            }
        }

        public HostStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }
        
        public Host(string name, string ipAddress, string macAddress)
        {
            Name = name;
            IpAddress = ipAddress;
            MacAddress = macAddress;
        }

        public Host(HostDb hostDb)
        {
            Name = hostDb.Name;
            IpAddress = hostDb.IpAddress;
            MacAddress = hostDb.MacAddress;
            Status = HostStatus.Unknown;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class HostDb
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
    }
}