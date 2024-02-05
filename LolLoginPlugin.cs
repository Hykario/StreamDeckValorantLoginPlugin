using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LolLogin
{
    [PluginActionId("com.zaphop.lollogin.lollogin")]
    public class LolLoginPlugin : KeypadBase
    {
        private static bool _inProgress = false;
        private static readonly object _syncObject = new object();

        private RSACng _rsaCng = null;

        private string _username = null;
        private string _encryptedPassword = null;
        private string _password = null; // This is always a string of * to match password length. 

        private event LolLoginManager.OnProgressUpdateDelegate OnProgressUpdate;

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

            // Send the dots back to the PI so that the user can see the password is set. The real decrypted password is never sent back to the PI. 
            if (e.Event.Payload.TryGetValue("property_inspector", out var value) == true && value.Value<String>() == "propertyInspectorConnected")
                Connection.SendToPropertyInspectorAsync(JObject.FromObject(new { password = _password }));
        }

        public override void KeyPressed(KeyPayload payload)
        {
            lock (_syncObject)
            {
                if (_inProgress == false)
                {
                    _inProgress = true;

                    Task.Run(() =>
                    {
                        string password;

                        try
                        {
                            var encryptedPasswordBytes = Convert.FromBase64String(_encryptedPassword);
                            var decryptedPasswordBytes = _rsaCng.Decrypt(encryptedPasswordBytes, RSAEncryptionPadding.Pkcs1);
                            password = ASCIIEncoding.UTF8.GetString(decryptedPasswordBytes);
                        }
                        catch (Exception)
                        {
                            // It is possible for the password decryption to fail if the user tries to move thier StreamDeck config 
                            // to a new computer. If this happens, blank the passwords so the can reset them.
                            _encryptedPassword = null;
                            _password = null;

                            Connection.SetSettingsAsync(JObject.FromObject(new
                            {
                                username = _username,
                                encrypted_password = _encryptedPassword,
                                password = _password
                            }));

                            _inProgress = false;

                            throw;
                        }

                        this.OnProgressUpdate += LolLoginPlugin_OnProgressUpdate;

                        var loginManager = new LolLoginManager();
                        loginManager.Login(_username, password, OnProgressUpdate);
                    });
                }
            }
        }

        private void LolLoginPlugin_OnProgressUpdate(double progress)
        {
            Bitmap bitmap = new Bitmap(72, 72);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Black);

            Brush white = new SolidBrush(Color.FromKnownColor(KnownColor.GhostWhite));
            Brush gradient = new LinearGradientBrush(new RectangleF(0, 0, bitmap.Width, bitmap.Height), Color.FromKnownColor(KnownColor.CornflowerBlue), Color.FromKnownColor(KnownColor.Crimson), 0F);

            graphics.FillRectangle(white, new RectangleF(0, bitmap.Height / 5 * 2, bitmap.Width, bitmap.Height / 5));

            graphics.FillRectangle(gradient, new RectangleF(1, (bitmap.Height / 5) * 2 + 1, (float) (bitmap.Width * progress) - 2, (bitmap.Height / 5) - 2));

            Connection.SetImageAsync(Tools.ImageToBase64(bitmap, true));

            if (progress == 1.0)
            {
                Connection.SetImageAsync(Tools.FileToBase64("Images\\action20.png", true));
                _inProgress = false;
            }
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
                })).Wait();
            }

            try
            {
                var cngKeyBytes = Convert.FromBase64String(privateKeyString);
                CngKey privateCng = CngKey.Import(cngKeyBytes, CngKeyBlobFormat.GenericPrivateBlob);

                _rsaCng = new RSACng(privateCng);
            }
            catch (Exception)
            {
                // If there is any error in the key serialization or rehydration of the key back into rsaCng, then
                // remove the bad key. A new key will be requested the next time the plugin is loaded. 
                // This should only happen if someone moves settings from an old profile onto a new machine. 
                Connection.SetGlobalSettingsAsync(JObject.FromObject(new
                {
                    rsaPrivateKey = ""
                })).Wait();

                throw;
            }
        }
    }
}