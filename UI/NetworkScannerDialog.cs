using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.UI
{
    public class NetworkScannerDialog : Form
    {
        public NetworkScannerControl ScannerControl { get; }

        public NetworkScannerDialog(List<string> existingGroups)
        {
            this.Text = "Network Scanner — Discover Remote Hosts & Services";
            this.Size = new Size(960, 680);
            this.MinimumSize = new Size(780, 520);
            this.StartPosition = FormStartPosition.CenterParent;

            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            this.BackColor = isDark ? Color.FromArgb(28, 30, 34) : Color.FromArgb(248, 249, 250);

            ScannerControl = new NetworkScannerControl(existingGroups);
            this.Controls.Add(ScannerControl);
        }
    }
}
