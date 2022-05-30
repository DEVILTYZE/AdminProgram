using System.Text.Encodings.Web;
using System.Text.Json;

namespace CommandLib
{
    public static class ConstHelper
    {
        public const int MessageCommandId = 0;
        public const int ShutdownCommandId = 1;
        public const int GetFileCommandId = 2;
        public const int StreamCommandId = 3;
        
        public const string MessageCommandString = "GET_MESSAGE";
        public const string ShutdownCommandString = "SHUTDOWN";
        public const string GetFileCommandString = "GET_FILE";
        public const string StreamCommandString = "STREAM";

        public const string DataError = "Невозможно получить IPEndPoint или RSAParameters.";
        public const string FileError = "Файла не существует.";
        public const string FileLengthError = "Вес файла больше допустимого (150 МБ).";

        public const int SleepTimeout = 100;
        
        public static readonly JsonSerializerOptions Options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, 
            WriteIndented = true
        };
        //public static byte[] Separator = { byte.MaxValue, byte.MaxValue, byte.MinValue, byte.MaxValue, byte.MinValue };
    }
}