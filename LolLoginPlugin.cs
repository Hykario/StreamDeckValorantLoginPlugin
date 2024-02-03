using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace LolLogin
{
    [PluginActionId("com.zaphop.lollogin.lollogin")]
    public class LolLoginPlugin : KeypadBase
    {
        private RSACng _rsaCng = null;

        private string _username = null;
        private string _encryptedPassword = null;
        private string _password = null; // This is always a string of * to match password length. 

        private Guid _instanceUUID = Guid.Empty;
        
        private void UpdateSettings(JObject settings)
        {
            _username = settings.Value<String>("username");
            _encryptedPassword = settings.Value<String>("encrypted_password");
            _password = settings.Value<String>("password");
        }

        public LolLoginPlugin(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            UpdateSettings(payload.Settings);

            connection.OnSendToPlugin += Connection_OnSendToPlugin;

            connection.GetGlobalSettingsAsync();
        }

        public override void Dispose()
        {
            Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
        }

        private void Connection_OnSendToPlugin(object sender, SDEventReceivedEventArgs<SendToPlugin> e)
        {
            var action = e.Event.Payload.Value<string>("action");
            
            if (action == "setPassword")
            {
                var password = e.Event.Payload.Value<string>("password");
                var passwordBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(password);
                var encryptedPasswordBytes = _rsaCng.Encrypt(passwordBytes, RSAEncryptionPadding.Pkcs1);
                var encryptedPasswordString = Convert.ToBase64String(encryptedPasswordBytes);

                _password = "".PadLeft(password.Length, '*');
                _encryptedPassword = encryptedPasswordString;

                Connection.SetSettingsAsync(JObject.FromObject(new
                {
                    username = _username,
                    encrypted_password = encryptedPasswordString,
                    password = _password
                }));
            }

            // Send the dots back to the PI so that the user can see the password is set.
            if (e.Event.Payload.TryGetValue("property_inspector", out var value) == true && value.Value<String>() == "propertyInspectorConnected")
                Connection.SendToPropertyInspectorAsync(JObject.FromObject(new { password = _password }));
        }

        public override void KeyPressed(KeyPayload payload)
        {
            var encryptedPasswordBytes = Convert.FromBase64String(_encryptedPassword);
            var decryptedPasswordBytes = _rsaCng.Decrypt(encryptedPasswordBytes, RSAEncryptionPadding.Pkcs1);
            var password = ASCIIEncoding.UTF8.GetString(decryptedPasswordBytes);

            var loginManager = new LolLoginManager();
            loginManager.Login(_username, password);
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            UpdateSettings(payload.Settings);
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            var privateKeyString = payload.Settings.Value<string>("rsaPrivateKey");
            if (string.IsNullOrEmpty(privateKeyString) == true)
            {
                RSACng encryptionKey = new RSACng();

                var privateKeyBytes = encryptionKey.Key.Export(CngKeyBlobFormat.GenericPrivateBlob);
                privateKeyString = Convert.ToBase64String(privateKeyBytes);

                Connection.SetGlobalSettingsAsync(JObject.FromObject(new
                {
                    rsaPrivateKey = privateKeyString
                }));
            }

            var cngKeyBytes = Convert.FromBase64String(privateKeyString);
            CngKey privateCng = CngKey.Import(cngKeyBytes, CngKeyBlobFormat.GenericPrivateBlob);

            _rsaCng = new RSACng(privateCng);
        }
    }
}