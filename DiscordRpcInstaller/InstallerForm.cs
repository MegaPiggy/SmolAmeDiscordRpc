using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordRpcInstaller
{
    public partial class InstallerForm : Form
    {
        public void SetIcon(Icon icon)
        {
            SuspendLayout();
            Icon = icon;
            ResumeLayout(false);
        }

        public InstallerForm()
        {
            InitializeComponent();
            SetTopLevel(true);
            TopLevel = true;
            BringToFront();
            CreateHandle();
        }
    }
}
