using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace DiscordRpcInstaller
{
    public static class Program
    {
        private static InstallerForm form;
        public static InstallerForm Form => form;

        public const string DataFolder = "Smol Ame_Data";

        public static bool IsAdmin
        {
            get
            {
                AppDomain myDomain = Thread.GetDomain();
                myDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                WindowsPrincipal myPrincipal = (WindowsPrincipal)Thread.CurrentPrincipal;
                return myPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            form = new InstallerForm();
            form.SetIcon(new System.Drawing.Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Program), "AmeliaDiscord.ico")));
            if (!IsAdmin)
            {
                MessageBox.Show(form, "Please run this installer as an administrator or it to work!", "Cannot install", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                goto dispose;
            }
            try
            {
                if (args != null && args.Length > 0) Directory.SetCurrentDirectory(File.Exists(args[0]) ? Directory.GetParent(args[0]).FullName : args[0]);
                if (!Directory.Exists(DataFolder))
                {
                    string path = Prompt.ShowDialog(form, "Select the directory of the game.", "Game not found");
                    if (!string.IsNullOrWhiteSpace(path)) Directory.SetCurrentDirectory(path);
                    else goto please;
                }
                if (Directory.Exists(DataFolder))
                {
                    if (!File.Exists(DataFolder + "/Managed/SALT.dll"))
                    {
                        DialogResult result = MessageBox.Show(form, $"Please install SALT for the mod to work.{Environment.NewLine}Would you like to go to the download page?", "Installed but won't work", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                        if (result == DialogResult.OK || result == DialogResult.Yes)
                            OpenUrl("https://www.nexusmods.com/smolame/mods/1?tab=files");
                    }
                    if (!Directory.Exists("SALT/Mods"))
                        Directory.CreateDirectory("SALT/Mods");

                    if (!ExtractSaveResourceReplace("SALT/Mods/RichPresence.dll", "RichPresence.dll", "SALT/Mods"))
                        goto dispose;

                    if (!Directory.Exists(DataFolder + "/Plugins/x86"))
                        Directory.CreateDirectory(DataFolder + "/Plugins/x86");
                    if (!Directory.Exists(DataFolder + "/Plugins/x86_64"))
                        Directory.CreateDirectory(DataFolder + "/Plugins/x86_64");

                    if (!ExtractSaveResourceReplace(DataFolder + "/Plugins/x86/discord-rpc.dll", "Plugins/x86/discord-rpc.dll", DataFolder + "/Plugins/x86"))
                        goto dispose;

                    if (!ExtractSaveResourceReplace(DataFolder + "/Plugins/x86_64/discord-rpc.dll", "Plugins/x86_64/discord-rpc.dll", DataFolder + "/Plugins/x86_64"))
                        goto dispose;

                    MessageBox.Show("Discord Rich Presence has been installed!", "Installed", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    goto dispose;
                }
                please:
                MessageBox.Show(form, "Please place this installer in the Smol Ame Game Folder for it to work!", "Cannot install", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);//Log("Please place this installer in the Smol Ame Game Folder for it to work!");
            }
            catch (Exception e)
            {
                MessageBox.Show(form, $"Please send a screenshot of this to the mod loader creator.{Environment.NewLine}{Environment.NewLine}{e.GetType().Name}: {e.Message}{Environment.NewLine}Stack Trace:{Environment.NewLine}{e.StackTrace.Replace("   at ", "")}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        dispose:
            form?.Dispose();
        }

        public static void ExtractSaveResource(string filename, string location)
        {
            string filelocation = filename.Replace("/", ".");
            filename = filename.Reverse().RemoveEverythingAfter("/").RemoveEverythingAfter("\\").Reverse();
            if (!location.EndsWith(filename.GetExtension()))
            {
                if (!(location.EndsWith(".") || location.EndsWith("/") || location.EndsWith("\\")))
                    location += "/";
                location += filename;
            }
            Stream resFilestream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Program), filelocation);
            if (resFilestream != null)
            {
                BinaryReader br = new BinaryReader(resFilestream);
                FileStream fs = new FileStream(location, FileMode.Create); // Say
                BinaryWriter bw = new BinaryWriter(fs);
                byte[] ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
                bw.Write(ba);
                br.Close();
                bw.Close();
                resFilestream.Close();
            }
        }

        public static bool ExtractSaveResourceReplace(string path, string filename, string location)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                ExtractSaveResource(filename, location);
                return true;
            }
            catch (UnauthorizedAccessException uaex)
            {
                MessageBox.Show(form, uaex.Message, "Cannot install", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return false;
            }
        }

        private static System.Drawing.Image GetImage(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Error:
                    return System.Drawing.SystemIcons.Error.ToBitmap();
                case MessageBoxIcon.Warning:
                    return System.Drawing.SystemIcons.Warning.ToBitmap();
                case MessageBoxIcon.Information:
                    return System.Drawing.SystemIcons.Information.ToBitmap();
                case MessageBoxIcon.Question:
                    return System.Drawing.SystemIcons.Question.ToBitmap();
            }
            return null;
        }

        public static void Log(string s)
        {
            Console.WriteLine(s);
            Console.ReadLine();
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// Sets clipboard to value.
        /// </summary>
        /// <param name="value">String to set the clipboard to.</param>
        public static void SetClipboard(this string value)
        {
            if (value == null)
                throw new ArgumentNullException("Attempt to set clipboard with null");

            Process clipboardExecutable = new Process();
            clipboardExecutable.StartInfo = new ProcessStartInfo // Creates the process
            {
                RedirectStandardInput = true,
                FileName = @"clip",
            };
            clipboardExecutable.Start();

            clipboardExecutable.StandardInput.Write(value); // CLIP uses STDIN as input.
            // When we are done writing all the string, close it so clip doesn't wait and get stuck
            clipboardExecutable.StandardInput.Close();

            return;
        }

        /// <summary>
        /// Reverses the string
        /// </summary>
        public static string Reverse(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static string RemoveEverythingAfter(this string str, string after)
        {
            int index = str.IndexOf(after);
            if (index >= 0)
                str = str.Substring(0, index);
            return str;
        }

        public static string GetExtension(this string file) => file.Reverse().RemoveEverythingAfter(".").Reverse();
    }
}
