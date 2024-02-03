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
    internal class SerializableRSAKeyParamaters
    {
        public string P { get; set; }
        public string Q { get; set; }
        public string DP { get; set; }
        public string DQ { get; set; }
        public string D { get; set; }
        public string Exponent { get; set; }
        public string InverseQ { get; set; }
        public string Modulus { get; set; }

        public SerializableRSAKeyParamaters()
        {
        }

        public SerializableRSAKeyParamaters(RSAParameters rsaParameters)
        {
            this.P = Convert.ToBase64String(rsaParameters.P);
            this.Q = Convert.ToBase64String(rsaParameters.Q);
            this.DP = Convert.ToBase64String(rsaParameters.DP);
            this.DQ = Convert.ToBase64String(rsaParameters.DQ);
            this.D = Convert.ToBase64String(rsaParameters.D);
            this.Exponent = Convert.ToBase64String(rsaParameters.Exponent);
            this.InverseQ = Convert.ToBase64String(rsaParameters.InverseQ);
            this.Modulus = Convert.ToBase64String(rsaParameters.Modulus);
        }

        public RSAParameters ToRsaParameters()
        {
            return new RSAParameters
            {
                P = Convert.FromBase64String(this.P),
                Q = Convert.FromBase64String(this.Q),
                DP = Convert.FromBase64String(this.DP),
                DQ = Convert.FromBase64String(this.DQ),
                D = Convert.FromBase64String(this.D),
                Exponent = Convert.FromBase64String(this.Exponent),
                InverseQ = Convert.FromBase64String(this.InverseQ),
                Modulus = Convert.FromBase64String(this.Modulus)
            };
        }
    }

    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }





            // RSA Hijinx 1
            //RSACng x = new RSACng();

            //var publicKey = x.Key.Export(CngKeyBlobFormat.GenericPublicBlob);
            //var privateKey = x.Key.Export(CngKeyBlobFormat.GenericPrivateBlob);

            //CngKey publicCng = CngKey.Import(publicKey, CngKeyBlobFormat.GenericPublicBlob);
            //CngKey privateCng = CngKey.Import(privateKey, CngKeyBlobFormat.GenericPrivateBlob);

            //RSACng encryptor = new RSACng(publicCng);

            //RSACng decryptor = new RSACng(privateCng);

            //var encrypted = decryptor.Encrypt(ASCIIEncoding.ASCII.GetBytes("pork!"), RSAEncryptionPadding.Pkcs1);

            //var decrypted = ASCIIEncoding.ASCII.GetString(x.Decrypt(encrypted, RSAEncryptionPadding.Pkcs1));




            // RSA Hijinx 2
            //var parms = x.ExportParameters(true);

            //SerializableRSAKeyParamaters storageParams = new SerializableRSAKeyParamaters(parms);

            //var jparms = JsonConvert.SerializeObject(storageParams);

            //var encrypted = x.Encrypt(ASCIIEncoding.ASCII.GetBytes("pork!"), RSAEncryptionPadding.Pkcs1);

            //var decrypted = ASCIIEncoding.ASCII.GetString(x.Decrypt(encrypted, RSAEncryptionPadding.Pkcs1));

            //RSACng y = new RSACng();
            //var yparms = JsonConvert.DeserializeObject<SerializableRSAKeyParamaters>(jparms);
            //y.ImportParameters(yparms.ToRsaParameters());

            //var decrypted2 = ASCIIEncoding.ASCII.GetString(y.Decrypt(encrypted, RSAEncryptionPadding.Pkcs1));




            //// Relaunch as Admin
            //var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            //var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            //if (isAdmin == false)
            //{
            //    var p = Process.GetCurrentProcess();
            //    var launchFile = p.MainModule.FileName;
            //    var commandLine = String.Join(" ", args.Take(args.Length - 1));

            //    commandLine += $" \"{args[args.Length - 1].Replace("\"", "\\\"")}\"";


            //    Win32.ShellExecute(IntPtr.Zero, "runas", launchFile, commandLine, ".", 5);
            //    return;
            //}



            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Win32.SetProcessDPIAware();

            SDWrapper.Run(args);
        }
    }
}
