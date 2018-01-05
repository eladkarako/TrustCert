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

