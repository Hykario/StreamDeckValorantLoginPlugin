using BarRaider.SdTools;
using LolLogin;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
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

            // TODO: Probably don't need this anymore... test later!
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            //Win32.SetProcessDPIAware();

            SDWrapper.Run(args);
        }
    }
}
