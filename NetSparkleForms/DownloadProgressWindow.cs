using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using NetSparkle.Interfaces;
using NetSparkle;
using NetSparkle.Events;

namespace NetSparkleForms
{
    /// <summary>
    /// A progress bar
    /// </summary>
    public partial class DownloadProgressWindow : Form, IDownloadProgress
    {
        /// <summary>
        /// Event to fire when the download UI is complete; tells you 
        /// if the install process should happen or not
        /// </summary>
        public event DownloadInstallEventHandler DownloadProcessCompleted;

        private bool _shouldLaunchInstallFileOnClose = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="item">The appcast item to use</param>
        /// <param name="applicationIcon">Your application Icon</param>
        public DownloadProgressWindow(AppCastItem item, Icon applicationIcon)
        {
            InitializeComponent();

            imgAppIcon.Image = applicationIcon.ToBitmap();
            Icon = applicationIcon;

            // init ui
            btnInstallAndReLaunch.Visible = false;
            lblHeader.Text = lblHeader.Text.Replace("APP", item.AppName + " " + item.Version);
            downloadProgressLbl.Text = "";
            progressDownload.Maximum = 100;
            progressDownload.Minimum = 0;
            progressDownload.Step = 1;

            FormClosing += DownloadProgressWindow_FormClosing;
        }

        private void DownloadProgressWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            FormClosing -= DownloadProgressWindow_FormClosing;
            DownloadProcessCompleted?.Invoke(this, new DownloadInstallArgs(_shouldLaunchInstallFileOnClose));
        }

        /// <summary>
        /// Show the UI and waits
        /// </summary>
        bool IDownloadProgress.ShowDialog()
        {
            return DefaultUIFactory.ConvertDialogResultToDownloadProgressResult(ShowDialog());
        }

        /// <summary>
        /// Update UI to show file is downloaded and signature check result
        /// </summary>
        public void FinishedDownloadingFile(bool isDownloadedFileValid)
        {
            progressDownload.Visible = false;
            buttonCancel.Visible = false;
            downloadProgressLbl.Visible = false;
            if (isDownloadedFileValid)
            {
                btnInstallAndReLaunch.Visible = true;
                BackColor = Color.FromArgb(240, 240, 240);
            }
            else
            {
                btnInstallAndReLaunch.Visible = false;
                BackColor = Color.Tomato;
            }
        }

        /// <summary>
        /// Display an error message
        /// </summary>
        /// <param name="errorMessage">The error message to display</param>
        public bool DisplayErrorMessage(string errorMessage)
        {
            downloadProgressLbl.Visible = true;
            progressDownload.Visible = false;
            btnInstallAndReLaunch.Visible = false;
            buttonCancel.Text = "Close";
            downloadProgressLbl.Text = errorMessage;
            return true;
        }

        /// <summary>
        /// Force window close
        /// </summary>
        public void ForceClose()
        {
            DialogResult = DialogResult.Abort;
            Close();
        }

        /// <summary>
        /// Event called when the client download progress changes
        /// </summary>
        private void OnDownloadProgressChanged(long bytesReceived, long totalBytesToReceive, int percentage)
        {
            progressDownload.Value = percentage;
            downloadProgressLbl.Text = " (" + Utilities.NumBytesToUserReadableString(bytesReceived) + " / " + 
                Utilities.NumBytesToUserReadableString(totalBytesToReceive) + ")";
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressChanged(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }

        /// <summary>
        /// Event called when the "Install and relaunch" button is clicked
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        private void OnInstallAndReLaunchClick(object sender, EventArgs e)
        {
            _shouldLaunchInstallFileOnClose = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// TODO
        /// </summary>
        public void SetDownloadAndInstallButtonEnabled(bool shouldBeEnabled)
        {
            btnInstallAndReLaunch.Enabled = shouldBeEnabled;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
