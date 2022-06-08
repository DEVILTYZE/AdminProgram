using System;
using System.Collections.Generic;
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
        public bool RightButtonIsPressed { get; set; }
        public byte LeftButtonClickCount { get; set; }
        public bool LeftButtonIsPressed { get; set; }
        public int Delta { get; set; }
        public byte[] Keys { get; set; }
        public byte[] States { get; set; }
        
        [JsonIgnore]
        public Queue<byte> KeysQueue { get; set; }
        [JsonIgnore]
        public Queue<byte> StatesQueue { get; set; }

        [JsonConstructor]
        public RemoteControlObject()
        {
            KeysQueue = new Queue<byte>();
            StatesQueue = new Queue<byte>();
        }

        public void ToStartCondition()
        {
            KeysQueue.Clear();
            StatesQueue.Clear();
            Delta = 0;
            LeftButtonClickCount = 0;
            RightButtonClickCount = 0;
        }
        
        public byte[] ToBytes()
        {
            Keys = KeysQueue.ToArray();
            States = StatesQueue.ToArray();
            
            return JsonSerializer.SerializeToUtf8Bytes(this, ConstHelper.Options);
        }

        public static RemoteControlObject FromBytes(byte[] data) =>
            JsonSerializer.Deserialize<RemoteControlObject>(data, ConstHelper.Options);
    }
}