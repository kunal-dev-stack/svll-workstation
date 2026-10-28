using System;
using System.Drawing;
using System.Windows.Forms;

namespace AegisMeshAgent
{
    public class SessionBannerForm : Form
    {
        public Action? OnEmergencyDisconnectRequested;

        public SessionBannerForm(string adminUser)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(15, 23, 42);
            Size = new Size(460, 48);

            // Position at top-right of primary screen
            var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            Location = new Point(screen.Right - Width - 20, screen.Top + 20);

            var lbl = new Label
            {
                Text = $"🛡️ AegisMesh Active Session: Managed by {adminUser}",
                ForeColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(330, 48),
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(14, 0)
            };

            var btnDisconnect = new Button
            {
                Text = "Disconnect",
                ForeColor = Color.FromArgb(251, 113, 133),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Size = new Size(88, 28),
                Location = new Point(356, 10),
                Cursor = Cursors.Hand
            };
            btnDisconnect.FlatAppearance.BorderColor = Color.FromArgb(244, 63, 94);
            btnDisconnect.Click += (s, e) =>
            {
                OnEmergencyDisconnectRequested?.Invoke();
                Close();
            };

            Controls.Add(lbl);
            Controls.Add(btnDisconnect);
        }

        protected override bool ShowWithoutActivation => true;
    }
}
