using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommandLib.Commands.RemoteCommandItems
{
    [Serializable]
    public class RemoteControlObject
    {
        public double MouseX { get; set; }
        public double MouseY { get; set; }
        public byte RightButtonClickCount { get; set; }
        public byte LeftButtonClickCount { get; set; }
        public int Delta { get; set; }
        public byte Key { get; set; }

        [JsonConstructor]
        public RemoteControlObject()
        {
        }

        public void ToStartState()
        {
            MouseX = 0;
            MouseY = 0;
            RightButtonClickCount = 0;
            LeftButtonClickCount = 0;
            Delta = 0;
            Key = 0;
        }

        public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, ConstHelper.Options);

        public static RemoteControlObject FromBytes(byte[] data) =>
            JsonSerializer.Deserialize<RemoteControlObject>(data, ConstHelper.Options);
    }
}