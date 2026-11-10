using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.UI
{
    public class QuickStartDashboard : UserControl
    {
        public event EventHandler? QuickConnectClicked;
        public event EventHandler? AddEntryClicked;
        public event EventHandler? ImportClicked;
        public event EventHandler? ManageVaultClicked;
        public event EventHandler? ProbeNetworkClicked;

        public QuickStartDashboard()
        {
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true;
            InitializeUI();
        }

        private void InitializeUI()
        {
            bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
            bool isDark = isMatrix || ThemeSettings.CurrentTheme != AppTheme.Light;

            Color bg = isMatrix ? Color.FromArgb(4, 8, 4) : (isDark ? Color.FromArgb(24, 25, 28) : Color.FromArgb(248, 249, 250));
            Color fg = isMatrix ? Color.FromArgb(0, 255, 102) : (isDark ? Color.FromArgb(235, 237, 240) : Color.FromArgb(32, 33, 36));
            Color subFg = isMatrix ? Color.FromArgb(0, 200, 80) : (isDark ? Color.FromArgb(160, 165, 175) : Color.FromArgb(100, 105, 115));
            Color cardBg = isMatrix ? Color.FromArgb(8, 16, 8) : (isDark ? Color.FromArgb(34, 36, 42) : Color.White);
            Color cardBorder = isMatrix ? Color.FromArgb(0, 180, 60) : (isDark ? Color.FromArgb(50, 54, 64) : Color.FromArgb(220, 224, 230));

            this.BackColor = bg;
            this.ForeColor = fg;

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoScroll = true,
                Padding = new Padding(30, 25, 30, 20)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            // Header Section
            Panel pnlHeader = new Panel { Dock = DockStyle.Fill };
            Label lblTitle = new Label
            {
                Text = "Quick start",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = fg,
                AutoSize = true,
                Location = new Point(4, 0)
            };
            Label lblSubtitle = new Label
            {
                Text = "Connect to a remote desktop, add entries, or manage your encrypted connection workspace",
                Font = new Font("Segoe UI", 10f),
                ForeColor = subFg,
                AutoSize = true,
                Location = new Point(6, 36)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // Three Cards Row
            TableLayoutPanel cardsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0)
            };
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            // Card 1: Quick Connect
            Panel card1 = CreateCard(
                iconSymbol: "⚡",
                iconColor: isMatrix ? Color.FromArgb(0, 255, 102) : Color.DodgerBlue,
                title: "Quick connect",
                description: "RDP, SSH, SDL3, Linux XRDP and more ad-hoc sessions.",
                buttonText: "Connect",
                isPrimary: true,
                cardBg: cardBg,
                cardBorder: cardBorder,
                fg: fg,
                subFg: subFg,
                isMatrix: isMatrix,
                onClick: (s, e) => QuickConnectClicked?.Invoke(this, EventArgs.Empty)
            );

            // Card 2: Workspace & Vault
            Panel card2 = CreateCard(
                iconSymbol: "🛡️",
                iconColor: isMatrix ? Color.FromArgb(0, 255, 170) : Color.Coral,
                title: "Secure workspace",
                description: "AES-256-GCM vault with optional master password and export.",
                buttonText: "Manage Vault",
                isPrimary: false,
                cardBg: cardBg,
                cardBorder: cardBorder,
                fg: fg,
                subFg: subFg,
                isMatrix: isMatrix,
                onClick: (s, e) => ManageVaultClicked?.Invoke(this, EventArgs.Empty)
            );

            // Card 3: Import Entries
            Panel card3 = CreateCard(
                iconSymbol: "📥",
                iconColor: isMatrix ? Color.FromArgb(0, 255, 102) : Color.RoyalBlue,
                title: "Import entries",
                description: "From Microsoft .rdp files, exported vault bundles, or scripts.",
                buttonText: "Import...",
                isPrimary: true,
                cardBg: cardBg,
                cardBorder: cardBorder,
                fg: fg,
                subFg: subFg,
                isMatrix: isMatrix,
                onClick: (s, e) => ImportClicked?.Invoke(this, EventArgs.Empty)
            );

            cardsLayout.Controls.Add(card1, 0, 0);
            cardsLayout.Controls.Add(card2, 1, 0);
            cardsLayout.Controls.Add(card3, 2, 0);

            // Bottom Actions Row
            FlowLayoutPanel bottomActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(4, 10, 0, 0)
            };

            Button btnAdd = new Button
            {
                Text = "➕  Add entry",
                Height = 36,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = cardBg,
                ForeColor = fg,
                Margin = new Padding(0, 0, 12, 0)
            };
            btnAdd.FlatAppearance.BorderColor = cardBorder;
            btnAdd.Click += (s, e) => AddEntryClicked?.Invoke(this, EventArgs.Empty);

            Button btnScan = new Button
            {
                Text = "🌐  Scan network",
                Height = 36,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = cardBg,
                ForeColor = fg
            };
            btnScan.FlatAppearance.BorderColor = cardBorder;
            btnScan.Click += (s, e) => ProbeNetworkClicked?.Invoke(this, EventArgs.Empty);

            bottomActions.Controls.Add(btnAdd);
            bottomActions.Controls.Add(btnScan);

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(cardsLayout, 0, 1);
            mainLayout.Controls.Add(bottomActions, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateCard(string iconSymbol, Color iconColor, string title, string description, string buttonText, bool isPrimary, Color cardBg, Color cardBorder, Color fg, Color subFg, bool isMatrix, EventHandler onClick)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                BackColor = cardBg,
                Padding = new Padding(18)
            };

            card.Paint += (s, e) => {
                using var pen = new Pen(cardBorder, 1.5f);
                var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = CreateRoundedRect(r, 8);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            };

            Label lblIcon = new Label
            {
                Text = iconSymbol,
                Font = new Font("Segoe UI", 26f),
                ForeColor = iconColor,
                AutoSize = true,
                Location = new Point(16, 16)
            };

            Label lblCardTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = fg,
                AutoSize = true,
                Location = new Point(16, 75)
            };

            Label lblCardDesc = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 9f),
                ForeColor = subFg,
                Location = new Point(18, 105),
                Size = new Size(240, 48)
            };

            Color btnBg = isMatrix
                ? (isPrimary ? Color.FromArgb(0, 80, 25) : Color.FromArgb(10, 25, 10))
                : (isPrimary ? Color.FromArgb(0, 120, 215) : Color.FromArgb(50, 55, 68));

            Color btnBorder = isMatrix 
                ? Color.FromArgb(0, 255, 102) 
                : (isPrimary ? Color.FromArgb(0, 100, 190) : cardBorder);

            Button btnAction = new Button
            {
                Text = buttonText,
                Location = new Point(18, 160),
                Width = 110,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = btnBg,
                ForeColor = isMatrix ? Color.FromArgb(0, 255, 102) : Color.White
            };
            btnAction.FlatAppearance.BorderColor = btnBorder;
            btnAction.FlatAppearance.BorderSize = 1;
            btnAction.Click += onClick;

            card.Controls.Add(lblIcon);
            card.Controls.Add(lblCardTitle);
            card.Controls.Add(lblCardDesc);
            card.Controls.Add(btnAction);

            return card;
        }

        private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
