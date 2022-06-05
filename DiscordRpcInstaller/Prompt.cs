using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace DiscordRpcInstaller
{
    public static class Prompt
    {
        public static string ShowDialog(IWin32Window owner, string message, string title)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Reset();

            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;

            // Set the help text description for the FolderBrowserDialog.
            folderBrowserDialog.Description = title + Environment.NewLine + message;

            // Do not allow the user to create new files via the FolderBrowserDialog.
            folderBrowserDialog.ShowNewFolderButton = false;

            DialogResult result = folderBrowserDialog.ShowDialog(owner);
            return result == DialogResult.OK ? folderBrowserDialog.SelectedPath : string.Empty;
        }
    }
}
