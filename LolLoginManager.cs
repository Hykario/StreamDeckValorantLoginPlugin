using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LolLogin
{
    internal class LolLoginManager
    {
        public void Login(string username)
        {
            var password = GetPasswordFromCredentialManager(username);

            KillRunningRiotProcesses();

            LaunchRiotClient();

            Win32.RECT rect = WaitForRiotClient(30);

            // TODO: This is probably a parameter for the user to set.
            Thread.Sleep(3000);

            var offsetX = 110F / 1536F * (rect.right - rect.left);
            var offsetY = 250F / 864F * (rect.bottom - rect.top);

            var usernameTextboxOffset = new Point((int)offsetX, (int)offsetY);

            Win32.SendLeftClick(new System.Drawing.Point(rect.left + usernameTextboxOffset.X, rect.top + usernameTextboxOffset.Y));

            KeySender.SendKeyPressToActiveApplication(Keys.A | Keys.Control);

            KeySender.SendString(username);

            KeySender.SendKeyPressToActiveApplication(Keys.Tab);

            KeySender.SendKeyPressToActiveApplication(Keys.A | Keys.Control);

            KeySender.SendString(password);

            for (int i = 0; i < 6; i++)
                KeySender.SendKeyPressToActiveApplication(Keys.Tab);

            KeySender.SendKeyPressToActiveApplication(Keys.Enter);
        }

        private string GetPasswordFromCredentialManager(string username)
        {
            var set = new CredentialManagement.CredentialSet();
            set.Load();

            var credential = set.FirstOrDefault(p => p.Target == $"{CredentialManager.CredentialManagementTypePrefix} - {username}" && p.Username == username);

            if (credential == null)
                throw new Exception("Password could not be found for user: " + username);

            return credential.Password;
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
                runningProcesses.Add(Process.Start($"taskkill", $"/f /im {item}"));

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
