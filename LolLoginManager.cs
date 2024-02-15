using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Exceptions;
using FlaUI.UIA3;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

                Process targetProcess = null;

                foreach (var proc in Process.GetProcessesByName("RIOT CLIENT"))
                {
                    ProcessCommandLine.Retrieve(proc, out string cl);

                    if (cl.Contains("--type=") == true)
                        continue;

                    targetProcess = proc;
                    break;
                }

                if (targetProcess == null)
                    throw new Exception("Couldn't find 'Riot Client.exe' with a main window handle.");

                var application = FlaUI.Core.Application.Attach(targetProcess);

                var mainWindow = application.GetMainWindow(new UIA3Automation());

                ConditionFactory cf = new ConditionFactory(new UIA3PropertyLibrary());

                onProgressUpdate(0.5);

                FlaUI.Core.AutomationElements.TextBox usernameTextbox = null;
                FlaUI.Core.AutomationElements.TextBox passwordTextbox = null;
                FlaUI.Core.AutomationElements.Button signinButton = null;

                for (int i = 0; i < 1000; i++)
                {
                    try
                    {
                        if (usernameTextbox == null || String.IsNullOrEmpty(usernameTextbox.AutomationId) == true)
                        {
                            var control = mainWindow.FindFirstDescendant(cf.ByAutomationId("username")).AsTextBox();
                            if (control != null && String.IsNullOrEmpty(control.AutomationId) == false)
                            {
                                usernameTextbox = control;
                                onProgressUpdate(0.6);
                            }
                        }
                    }
                    catch (PropertyNotSupportedException)
                    {
                    }

                    try
                    {
                        if (passwordTextbox == null || String.IsNullOrEmpty(passwordTextbox.AutomationId) == true)
                        {
                            var control = mainWindow.FindFirstDescendant(cf.ByAutomationId("password")).AsTextBox();
                            if (control != null && String.IsNullOrEmpty(control.AutomationId) == false)
                            {
                                passwordTextbox = control;
                                onProgressUpdate(0.7);
                            }
                        }
                    }
                    catch (PropertyNotSupportedException)
                    {
                    }

                    try
                    {
                        if (signinButton == null)
                        {
                            var buttons = mainWindow.FindAll(TreeScope.Descendants, cf.ByControlType(ControlType.Button));

                            // The sign in button has no name and is also almost square. The below should find it at all resolutions.
                            var button = buttons.FirstOrDefault(p => String.IsNullOrEmpty(p.Name) == true && Math.Abs(p.BoundingRectangle.Width - p.BoundingRectangle.Height) < 10);

                            if (button != null)
                            {
                                signinButton = button.AsButton();
                                onProgressUpdate(0.8);
                            }
                        }
                    }
                    catch (PropertyNotSupportedException)
                    {
                    }

                    try
                    {
                        if (usernameTextbox != null
                            && passwordTextbox != null
                            && signinButton != null)
                        {
                            // Final checks to ensure controls are in states that we can interact with. 
                            FlaUI.Core.Input.Wait.UntilResponsive(usernameTextbox, TimeSpan.FromMilliseconds(10000));
                            FlaUI.Core.Input.Wait.UntilResponsive(passwordTextbox, TimeSpan.FromMilliseconds(10000));
                            var patternTest = signinButton.InvokePattern.EventIds;

                            // Everything is ready, we can leave the loop.
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        // Reset the controls and attempt to find current versions.
                        usernameTextbox = null;
                        passwordTextbox = null;
                        signinButton = null;
                        onProgressUpdate(0.5);
                    }


                    Thread.Sleep(100);
                }

                if (usernameTextbox == null)
                    throw new Exception("Couldn't find username text box.");

                if (passwordTextbox == null)
                    throw new Exception("Couldn't find password text box.");

                if (signinButton == null)
                    throw new Exception("Couldn't find sign in button.");

                onProgressUpdate(0.9);

                usernameTextbox.Text = username;
                passwordTextbox.Text = password;
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
                var procs = Process.GetProcessesByName("RIOT CLIENT");

                foreach (var proc in procs)
                {
                    Win32.RECT rect = new Win32.RECT();
                    if (Win32.GetWindowRect(proc.MainWindowHandle, ref rect))
                    {
                        Win32.SetForegroundWindowNative(proc.MainWindowHandle);
                        return rect;
                    }
                }


                //if (procs.Length > 0)
                //{
                //    var proc = procs[0];
                //    Win32.RECT rect = new Win32.RECT();
                //    if (Win32.GetWindowRect(proc.MainWindowHandle, ref rect))
                //    {
                //        Win32.SetForegroundWindowNative(proc.MainWindowHandle);
                //        return rect;
                //    }
                //}
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
                "\"League Of Legends.exe\"",
                "\"Riot Client.exe\""
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
