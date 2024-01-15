using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LolLogin
{
    [PluginActionId("com.zaphop.lollogin.lollogin")]
    public class LolLoginPlugin : KeypadBase
    {
        private SDConnection _connection;
        private string _username = null;

        private void UpdateSettings(JObject settings)
        {
            _username = settings.Value<String>("username");

            
        }

        public LolLoginPlugin(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            UpdateSettings(payload.Settings);

            _connection = connection;

            connection.OnSendToPlugin += Connection_OnSendToPlugin;
            
            //connection.GetGlobalSettingsAsync();

        }

        private void Connection_OnSendToPlugin(object sender, SDEventReceivedEventArgs<SendToPlugin> e)
        {
            var action = e.Event.Payload.Value<string>("action");
            if (action == "showCredentialManager")
            {
                WindowWrapper mainProcWindow = new WindowWrapper(ParentProcessUtilities.GetParentProcess().MainWindowHandle);
                new CredentialManager().ShowDialog(mainProcWindow);
                SendCurrentUserList(_connection);
            }

            if (action == "showHelp")
            {
                //Win32.ShellExecute(IntPtr.Zero, "Help.html", "", "", ".", 5);
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(Path.Combine(Directory.GetCurrentDirectory(), "Help.html"))
                {
                    UseShellExecute = true
                };
                p.Start();
            }

            if (e.Event.Payload.TryGetValue("property_inspector", out var value) == true && value.Value<String>() == "propertyInspectorConnected")
                SendCurrentUserList((SDConnection)sender);
        }

        private void SendCurrentUserList(SDConnection sender)
        {
            var set = new CredentialManagement.CredentialSet();
            set.Load();

            var userNameList = set
                .Where(p => p.Target.ToString().StartsWith(CredentialManager.CredentialManagementTypePrefix) == true)
                .Select(p => new { Name = p.Username, ID = p.Username });


            var userData = new { UserNames = userNameList, SelectedUserName = _username };
            var jobjectDevices = JObject.FromObject(userData);

            _connection.SendToPropertyInspectorAsync(jobjectDevices);
        }

        public override void Dispose()
        {
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Debug.WriteLine(_username);
            
            var loginManager = new LolLoginManager();
            loginManager.Login(_username);
        }

        public override void KeyReleased(KeyPayload payload) { }


        public override void OnTick()
        {
          
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            UpdateSettings(payload.Settings);

        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) 
        {
            UpdateSettings(payload.Settings);
        }
    }
}