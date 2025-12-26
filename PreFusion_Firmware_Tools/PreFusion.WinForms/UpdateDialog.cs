using PreFusion.Core;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Ext2Read.WinForms
{
    public class UpdateDialog : Form
    {
        private ReleaseInfo _release;
        private Label lblTitle;
        private Label lblVersion;
        private TextBox txtChangelog;
        private Button btnInstall;
        private Button btnCancel;
        private ProgressBar progressBar;

        public UpdateDialog(ReleaseInfo release)
        {
            _release = release;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Update Available";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblTitle = new Label 
            { 
                Text = "A new version of PreFusion is available!", 
                Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true 
            };
            this.Controls.Add(lblTitle);

            lblVersion = new Label 
            { 
                Text = $"New Version: {_release.TagName} (Current: {System.Reflection.Assembly.GetEntryAssembly().GetName().Version})",
                Location = new Point(20, 50),
                AutoSize = true
            };
            this.Controls.Add(lblVersion);

            var lblNotes = new Label { Text = "Release Notes:", Location = new Point(20, 80), AutoSize = true };
            this.Controls.Add(lblNotes);

            txtChangelog = new TextBox
            {
                Location = new Point(20, 100),
                Width = 440,
                Height = 180,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = _release.Body.Replace("\r\n", Environment.NewLine).Replace("\n", Environment.NewLine),
                BackColor = SystemColors.Window
            };
            this.Controls.Add(txtChangelog);

            progressBar = new ProgressBar 
            { 
                Location = new Point(20, 290), 
                Width = 440, 
                Height = 20, 
                Visible = false 
            };
            this.Controls.Add(progressBar);

            btnInstall = new Button 
            { 
                Text = "Install Update", 
                Location = new Point(230, 320), 
                Width = 120, 
                Height = 30,
                DialogResult = DialogResult.None 
            };
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            btnCancel = new Button 
            { 
                Text = "Remind Me Later", 
                Location = new Point(360, 320), 
                Width = 100, 
                Height = 30,
                DialogResult = DialogResult.Cancel 
            };
            this.Controls.Add(btnCancel);
        }

        private async void BtnInstall_Click(object sender, EventArgs e)
        {
            // Find .exe asset
            var asset = _release.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            
            if (asset == null)
            {
                MessageBox.Show("Installer not found in this release.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnInstall.Enabled = false;
            btnCancel.Enabled = false;
            progressBar.Visible = true;
            lblTitle.Text = "Downloading update...";

            var progress = new Progress<float>(p => 
            {
                progressBar.Value = (int)(p * 100);
            });

            try
            {
                await UpdateChecker.DownloadAndInstallAsync(asset.DownloadUrl, progress);
                
                // Close app to allow installer to run
                Application.Exit(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInstall.Enabled = true;
                btnCancel.Enabled = true;
                progressBar.Visible = false;
                lblTitle.Text = "Update Available";
            }
        }
    }
}
