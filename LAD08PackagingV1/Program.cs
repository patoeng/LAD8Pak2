using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace LAD08PackagingV1
{
    static class Program
    {
        [STAThread]
        static void Main()
        {

            string mutexId = $"Global\\{{ECD510AF-2E5C-4BC0-8826-6635B28EB987}}";
            bool createdNew;
            var allowEveryoneRule =
                new MutexAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);


            using (var mutex = new Mutex(false, mutexId, out createdNew, securitySettings))
            {
                var hasHandle = mutex.WaitOne(1000, false);
                if (!hasHandle)
                {
                    MessageBox.Show(@"Aplikasi sudah berjalan.");
                }
                else
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());
                }



                if (hasHandle)
                    mutex.ReleaseMutex();

            }
        }
    }
}
