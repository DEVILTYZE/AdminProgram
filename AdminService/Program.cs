using System.ServiceProcess;

namespace AdminService
{
    internal static class Program
    {
        private static void Main()
        {
            ServiceBase[] serviceToRun = { new AdminService() };
            ServiceBase.Run(serviceToRun);
        }
    }
}