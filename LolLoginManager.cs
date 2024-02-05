using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace LolLogin
{
    internal class LolLoginManager
    {
        public delegate void OnProgressUpdateDelegate(double progress);

        public void Login(string username, string password, OnProgressUpdateDelegate onProgressUpdate)
        {
            try
            {
                onProgressUpdate(0.0);

                KillRunningRiotProcesses();

                onProgressUpdate(0.1);

                LaunchRiotClient();

                onProgressUpdate(0.2);

                WaitForRiotClient(30);

                onProgressUpdate(0.3);

                Process process = Process.GetProcessesByName("RiotClientUx")[0];

                var application = FlaUI.Core.Application.Attach(process);

                var mainWindow = application.GetMainWindow(new UIA3Automation());

                FlaUI.Core.Input.Wait.UntilResponsive(mainWindow.FindFirstChild(), TimeSpan.FromMilliseconds(10000));
                ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

                onProgressUpdate(0.5);

                for (int i = 0; i < 30 * 100 && mainWindow.FindFirstDescendant(cf.ByAutomationId("username")) == null || mainWindow.FindFirstDescendant(cf.ByAutomationId("username")).AsTextBox() == null; i++)
                    Thread.Sleep(100);

                onProgressUpdate(0.6);

                var usernameTextbox = mainWindow.FindFirstDescendant(cf.ByAutomationId("username")).AsTextBox();
                usernameTextbox.Text = username;
                Thread.Sleep(100);

                for (int i = 0; i < 30 * 100 && mainWindow.FindFirstDescendant(cf.ByAutomationId("password")) == null || mainWindow.FindFirstDescendant(cf.ByAutomationId("password")).AsTextBox() == null; i++)
                    Thread.Sleep(100);

                onProgressUpdate(0.75);

                var passwordTextbox = mainWindow.FindFirstDescendant(cf.ByAutomationId("password")).AsTextBox();
                passwordTextbox.Text = password;
                Thread.Sleep(100);

                for (int i = 0; i < 30 * 100 && mainWindow.FindAllDescendants(cf.ByName("Sign in")) == null || mainWindow.FindAllDescendants(cf.ByName("Sign in")).Length < 1; i++)
                    Thread.Sleep(100);

                onProgressUpdate(0.9);

                // Find the first button after the Sign in text. Riot removed the Automation ID from it
                // so we have to hunt it down.
                var descendant = mainWindow.FindFirstDescendant(cf.ByName("Sign in"));
                var children = descendant.Parent.Parent.FindAllChildren();

                Button signinButton = null;

                for (int i = 0; i < children.Length; i++)
                {
                    // Making a big assumption that the login button is always directly after the
                    // stay signed in checkbox. Might have to change this to look for a button 
                    // with square dimensions as the look/feel of the login dialog hasn't changed in years.
                    if (children[i].AsCheckBox().Text == "Stay signed in")
                        signinButton = children[i + 1].AsButton();
                }

                if (signinButton == null)
                    throw new Exception("Couldn't find signin button on Login form.");

                signinButton.Invoke();
            }
            finally
            {
                onProgressUpdate(1.0);
            }
        }

        private static Win32.RECT WaitForRiotClient(int secondsToWait)
        {
            for (int i = 0; i <= secondsToWait * 1000 / 100; i++)
            {
                var procs = Process.GetProcessesByName("RiotClientUx");

                if (procs.Length > 0)
                {
                    var proc = procs[0];
                    Win32.RECT rect = new Win32.RECT();
                    if (Win32.GetWindowRect(proc.MainWindowHandle, ref rect))
                    {
                        Win32.SetForegroundWindowNative(proc.MainWindowHandle);
                        return rect;
                    }
                }
                Thread.Sleep(100);
            }

            throw new Exception("Riot Client did not start after waiting 30 seconds.");
        }

        private static void LaunchRiotClient()
        {
            var subKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Riot Game Riot_Client.";

            string installLocation;

            using (var key = Registry.CurrentUser.OpenSubKey(subKey, false))
            {
                if (key == null)
                    throw new Exception($"League of Legends is not installed, or missing uninstall inforamation: {subKey}");

                installLocation = key.GetValue("InstallLocation") as string;
            }

            if (installLocation == null)
                throw new Exception($"install location subkey not found in HKLM\\{subKey}");

            var fullName = new DirectoryInfo(Path.Combine(installLocation, "RiotClientServices.exe")).FullName;

            Process.Start(fullName, "--launch-product=league_of_legends --launch-patchline=live");
        }

        private static void KillRunningRiotProcesses()
        {
            var processes = new List<String>(new string[] {
                "riotclientcrashhandler.exe",
                "riotclientservices.exe",
                "riotclientux.exe",
                "riotclientuxrender.exe",
                "leagueclient.exe",
                "leagueclientux.exe",
                "leagueclientuxrender.exe",
                "leaguecrashhandler64.exe",
                "\"League Of Legends.exe\""
            });

            var runningProcesses = new List<Process>();

            foreach (var item in processes)
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = "taskkill";
                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmd.StartInfo.Arguments = $"/f /im {item}";
                cmd.Start();
                
                runningProcesses.Add(cmd);
            }

            while (true)
            {
                var processesStillRunning = false;

                foreach (var item in runningProcesses)
                {
                    if (item.HasExited == false)
                    {
                        processesStillRunning = true;
                    }
                }

                if (processesStillRunning == false)
                    return;

                Thread.Sleep(100);
            }
        }
    }
}
