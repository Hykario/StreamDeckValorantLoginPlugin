using BarRaider.SdTools;
using LolLogin;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace Visualizer
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (isAdmin == false)
            {
                var p = Process.GetCurrentProcess();
                var launchFile = p.MainModule.FileName;
                var commandLine = String.Join(" ", args.Take(args.Length - 1));

                commandLine += $" \"{args[args.Length - 1].Replace("\"", "\\\"")}\"";


                Win32.ShellExecute(IntPtr.Zero, "runas", launchFile, commandLine, ".", 5);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Win32.SetProcessDPIAware();

            SDWrapper.Run(args);
        }
    }
}
