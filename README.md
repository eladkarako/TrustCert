<h1>TrustCert</h1>
<h3>Problematically Trust Any Certificate. <br/>Self-Signed certificates too. <br/>Root-Trust. <br/>Install and uninstall. <br/>Can be easily intergated into batch-files, using the no prompt argument. <br/><sub><em>Full Source Code, Reversed engineered from Telerik's TrustCert.exe for education reasons</em>.</sub></h3>

<br/>

How to use:<br/>
You first can specify <code>-u -noprompt -path=C:\path\to\certificate.cer</code> first - to uninstall,<br/>
and then <code>-noprompt -path=C:\path\to\certificate.cer</code> to install recent-one.<br/>

Without <code>-noprompt</code> you'll be presented with simple ok/cancel message-box.
<br/>

How it's done:<br/>
Note: Source-code is really short, see <code>TrustCert/Program.cs</code>,<br/>
essentially using <code>System.Security.Cryptography.X509Certificates;</code> to open a X509Store, using <code>FindBySubjectDistinguishedName</code> to find a certificate by subject,<br/>
then using <code>X509Store</code>'s either <code>Add</code> or <code>Remove</code>, following again with a lookup to verify the certificate has added/removed successfully.
<br/>

The exe must be ran as an admin, if you're not the admin of your local-machine there is not much you can do, it is written in its manifest too:
<pre>
&lt;assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0"&gt;&lt;trustInfo xmlns="urn:schemas-microsoft-com:asm.v2"&gt;&lt;security&gt;&lt;requestedPrivileges&gt;&lt;<strong>requestedExecutionLevel level="requireAdministrator"</strong>&gt;&lt;/requestedExecutionLevel&gt;&lt;/requestedPrivileges&gt;&lt;/security&gt;&lt;/trustInfo&gt;&lt;compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1"&gt;&lt;application&gt;&lt;supportedOS Id="{e2011457-1546-43c5-a5fe-008deee3d3f0}"&gt;&lt;/supportedOS&gt;&lt;supportedOS Id="{35138b9a-5d96-4fbd-8e2d-a2440225f93a}"&gt;&lt;/supportedOS&gt;&lt;supportedOS Id="{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}"&gt;&lt;/supportedOS&gt;&lt;supportedOS Id="{1f676c76-80e1-4239-95bb-83d0f6d0da78}"&gt;&lt;/supportedOS&gt;&lt;supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}"&gt;&lt;/supportedOS&gt;&lt;/application&gt;&lt;/compatibility&gt;&lt;/assembly&gt;
</pre>

<br/>

<h3><em>may be combined with <a href="https://github.com/eladkarako/sign-exe/">https://github.com/eladkarako/sign-exe/</a>.</em></h3>

<hr/>

```c#
namespace TrustCert
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Windows.Forms;

    internal static class Program
    {
        private const string PathCommandLineParameter = "-path=";

        private static X509Certificate2Collection FindCertsBySubject(StoreName storeName, StoreLocation storeLocation, string sFullSubject)
        {
            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.OpenExistingOnly);
            store.Close();
            return store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, sFullSubject, false);
        }

        [STAThread]
        private static int Main(string[] sArgs)
        {
            string str;
            X509Certificate2 certificate;
            Application.EnableVisualStyles();
            if (sArgs.Length < 1)
            {
                MessageBox.Show("Syntax:\r\n\tTrustCert.exe [-noprompt] [-u] (CertSubject | -path=PathToCertificate)", "Incorrect Parameters");
                return 1;
            }
            CommandLineArguments arguments = ParseCommandLineArguments(sArgs, out str);
            bool flag = arguments.HasFlag(CommandLineArguments.Uninstall);
            if (arguments.HasFlag(CommandLineArguments.RootSubject))
            {
                StoreLocation storeLocation = flag ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
                X509Certificate2Collection certificates = FindCertsBySubject(StoreName.Root, storeLocation, str);
                if (certificates.Count < 1)
                {
                    MessageBox.Show($"Failed to find the root certificate in {flag ? "Machine" : "User"} Root List.", "TrustCert Failed");
                    if (flag)
                    {
                        return 0;
                    }
                    return 2;
                }
                certificate = certificates[0];
            }
            else
            {
                if (arguments.HasFlag(CommandLineArguments.PathToCertificate))
                {
                    if (!File.Exists(str))
                    {
                        MessageBox.Show("No certificate found at the following path:" + Environment.NewLine + Environment.NewLine + str, "Certificate not found");
                        return 5;
                    }
                    try
                    {
                        certificate = new X509Certificate2(str);
                        goto Label_0119;
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show("Error reading certificate from path - " + exception.Message, "Error reading certificate");
                        return 6;
                    }
                }
                MessageBox.Show("In order to add/remove a certificate to/from the Machine Root list you must provide either CertSubject or -path=PathToCertificate as last parameter.");
                return 7;
            }
        Label_0119:
            if (!setMachineTrust(certificate, !flag, !arguments.HasFlag(CommandLineArguments.NoPrompt)))
            {
                MessageBox.Show($"Failed to {flag ? "remove" : "add"} the root certificate {flag ? "from" : "to"} the Machine Root List.", "TrustCert Failed");
                if (!flag)
                {
                    return 3;
                }
                return 4;
            }
            MessageBox.Show($"{flag ? "Removed" : "Added"} Fiddler's root certificate {flag ? "from" : "to"} the Machine Root List.", "TrustCert Success");
            return 0;
        }

        private static CommandLineArguments ParseCommandLineArguments(string[] sArgs, out string stringParam)
        {
            CommandLineArguments none = CommandLineArguments.None;
            stringParam = sArgs[sArgs.Length - 1];
            if (stringParam.StartsWith("-path="))
            {
                none |= CommandLineArguments.PathToCertificate;
                stringParam = stringParam.Substring("-path=".Length);
            }
            else
            {
                none |= CommandLineArguments.RootSubject;
            }
            for (int i = 0; i < (sArgs.Length - 1); i++)
            {
                if (sArgs[i].StartsWith("/u", StringComparison.OrdinalIgnoreCase) || sArgs[i].StartsWith("-u", StringComparison.OrdinalIgnoreCase))
                {
                    none |= CommandLineArguments.Uninstall;
                }
                else if (sArgs[i] == "-noprompt")
                {
                    none |= CommandLineArguments.NoPrompt;
                }
            }
            return none;
        }

        private static bool setMachineTrust(X509Certificate2 oRootCert, bool bEnableTrust, bool shouldAskForConfirmation)
        {
            if (oRootCert == null)
            {
                return false;
            }
            if (shouldAskForConfirmation && (DialogResult.Yes != MessageBox.Show($"Please, confirm that you wish to {bEnableTrust ? "ADD" : "REMOVE"} the following certificate {bEnableTrust ? "to" : "from"} your PC's Trusted Root List:

	{oRootCert.Subject.ToString().Replace(", ", "\r\n\t")}", "TrustCert Confirmation", MessageBoxButtons.YesNo)))
            {
                return false;
            }
            try
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    if (bEnableTrust)
                    {
                        store.Add(oRootCert);
                    }
                    else
                    {
                        store.Remove(oRootCert);
                    }
                }
                finally
                {
                    store.Close();
                }
                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "TrustCert Failed");
                Trace.WriteLine("[FiddlerTrustCert] Failed to remove Machine roots: " + exception.Message);
                return false;
            }
        }

        [Flags]
        private enum CommandLineArguments
        {
            None = 0,
            NoPrompt = 2,
            PathToCertificate = 8,
            RootSubject = 4,
            Uninstall = 1
        }
    }
}

```
