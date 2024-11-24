using BarRaider.SdTools;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Exceptions;
using FlaUI.UIA3;
using LolLogin;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
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

			// Uncomment this to run a quick check without restarting LoL client.
			//TestHarness();

			SDWrapper.Run(args);
        }

        private static void TestHarness()
        {
			var loginManager = new LolLoginManager();
			loginManager.Login(false, "Pork", "Muffins", (e) => { });
		}
    }
}
