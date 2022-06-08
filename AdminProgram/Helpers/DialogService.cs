using Microsoft.Win32;

namespace AdminProgram.Helpers
{
    public static class DialogService
    {
        public static string FilePath { get; private set; }

        public static bool OpenFileDialog()
        {
            var ofd = new OpenFileDialog();
            var resultDialog = ofd.ShowDialog();

            if (!resultDialog.HasValue || !resultDialog.Value) 
                return false;
            
            FilePath = ofd.FileName;

            return true;
        }
    }
}