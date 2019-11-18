using NetSparkle;
using NetSparkle.Events;
using NetSparkle.Interfaces;
using System;
using System.Net;
using System.Windows;

namespace NetSparkleWPF.Windows
{
    /// <summary>
    /// Interaction logic for DownloadProgressWindow.xaml
    /// </summary>
    public partial class DownloadProgressWindow : Window, IDownloadProgress
    {
        DownloadProgressViewModel Context { get; }
        public DownloadProgressWindow(AppCastItem item, Uri applicationIcon)
        {
            Context = new DownloadProgressViewModel(item, applicationIcon);
            InitializeComponent();
            DataContext = Context;
            Context.DownloadProcessCompleted += InstallCommand;
        }

        private void CancelCommand(object sender, EventArgs e)
        {
            Close();
        }

        private void InstallCommand(object sender, DownloadInstallArgs e)
        {
            DownloadProcessCompleted?.Invoke(this, e);
            Close();
        }

        /// <summary>
        /// Event to fire when the download UI is complete; tells you 
        /// if the install process should happen or not
        /// </summary>
        public event DownloadInstallEventHandler DownloadProcessCompleted;

        public void SetDownloadAndInstallButtonEnabled(bool shouldBeEnabled)
        {
            Context.InstallButtonEnabled = shouldBeEnabled;
        }

        bool IDownloadProgress.ShowDialog()
        {
            return ShowDialog() ?? false;
        }

        public void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Context.UpdateProgress(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
        }

        public void ForceClose()
        {
            Close();
        }

        public void FinishedDownloadingFile(bool isDownloadedFileValid)
        {
            Context.FinishedDownloadingFile(isDownloadedFileValid);
        }

        public bool DisplayErrorMessage(string errorMessage)
        {
            return Context.ShowErrorMessage(errorMessage);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
