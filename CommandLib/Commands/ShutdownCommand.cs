using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using CommandLib.Commands.Helpers;

namespace CommandLib.Commands
{
    [Serializable]
    public class ShutdownCommand : AbstractCommand
    {
        [JsonConstructor]
        public ShutdownCommand() { }
        
        public ShutdownCommand(byte[] data, RSAParameters? publicKey = null) 
            : base(ConstHelper.ShutdownCommandId, ConstHelper.ShutdownCommandString, data, publicKey) { }

        public override CommandResult Execute()
        {
            var shutdownProcess = new Process
            {
                StartInfo =
                {
                    FileName = "shutdown.exe",
                    Arguments = @"-s -f -t 1",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                }
            };

            bool isOff;
            
            try
            {
                isOff = shutdownProcess.Start();
            }
            catch (Exception)
            {
                isOff = false;
            }

            return new CommandResult(isOff ? CommandResultStatus.Successed : CommandResultStatus.Failed,
                Array.Empty<byte>());
        }
    }
}