using System;
using NetSparkle.Interfaces;
using NetSparkleWPF.Windows;
using System.Collections.Generic;
using NetSparkleWPF.Interfaces;
using NetSparkle;
using System.Windows;

namespace NetSparkleWPF
{
    /// <summary>
    /// UI factory for default interface
    /// </summary>
    public class DefaultUIFactory : IWpfFactory
    {
        /// <summary>
        /// Parent window for pop ups
        /// </summary>
        public Window ParentWindow { get; set; }

        /// <summary>
        /// Create sparkle form implementation
        /// </summary>
        /// <param name="sparkle">The <see cref="SparkleWPF"/> instance to use</param>
        /// <param name="updates">Sorted array of updates from latest to earliest</param>
        /// <param name="applicationIcon">The icon to display</param>
        /// <param name="isUpdateAlreadyDownloaded">If true, make sure UI text shows that the user is about to install the file instead of download it.</param>
        public virtual IUpdateAvailable CreateSparkleForm(Sparkle sparkle, List<AppCastItem> updates, Uri applicationIcon, bool isUpdateAlreadyDownloaded = false)
        {
            return new UpdateAvailableWindow(sparkle, updates, applicationIcon, isUpdateAlreadyDownloaded) { Owner = ParentWindow };
        }

        /// <summary>
        /// Create download progress window
        /// </summary>
        /// <param name="item">Appcast item to download</param>
        /// <param name="applicationIcon">Application icon to use</param>
        public virtual IDownloadProgress CreateProgressWindow(AppCastItem item, Uri applicationIcon)
        {
            return new DownloadProgressWindow(item, applicationIcon) { Owner = ParentWindow };
        }

        /// <summary>
        /// Inform user in some way that NetSparkle is checking for updates
        /// </summary>
        /// <param name="applicationIcon">The icon to display</param>
        public virtual ICheckingForUpdates ShowCheckingForUpdates(Uri applicationIcon = null)
        {
            return new CheckingForUpdatesWindow(applicationIcon) { Owner = ParentWindow };
        }

        /// <summary>
        /// Initialize UI. Called when Sparkle is constructed.
        /// </summary>
        public virtual void Init()
        {
            // enable visual style to ensure that we have XP style or higher
            // also in WPF applications
            //Application.EnableVisualStyles();
        }

        /// <summary>
        /// Show user a message saying downloaded update format is unknown
        /// </summary>
        /// <param name="downloadFileName">The filename to be inserted into the message text</param>
        /// <param name="applicationIcon">The icon to display</param>
        public virtual void ShowUnknownInstallerFormatMessage(string downloadFileName, Uri applicationIcon = null)
        {
            ShowMessage(Properties.Resources.DefaultUIFactory_MessageTitle, 
                string.Format(Properties.Resources.DefaultUIFactory_ShowUnknownInstallerFormatMessageText, downloadFileName), applicationIcon);
        }

        /// <summary>
        /// Show user that current installed version is up-to-date
        /// </summary>
        public virtual void ShowVersionIsUpToDate(Uri applicationIcon = null)
        {
            ShowMessage(Properties.Resources.DefaultUIFactory_MessageTitle, Properties.Resources.DefaultUIFactory_ShowVersionIsUpToDateMessage, applicationIcon);
        }

        /// <summary>
        /// Show message that latest update was skipped by user
        /// </summary>
        public virtual void ShowVersionIsSkippedByUserRequest(Uri applicationIcon = null)
        {
            ShowMessage(Properties.Resources.DefaultUIFactory_MessageTitle, Properties.Resources.DefaultUIFactory_ShowVersionIsSkippedByUserRequestMessage, applicationIcon);
        }

        /// <summary>
        /// Show message that appcast is not available
        /// </summary>
        /// <param name="appcastUrl">the URL for the appcast file</param>
        /// <param name="applicationIcon">The icon to display</param>
        public virtual void ShowCannotDownloadAppcast(string appcastUrl, Uri applicationIcon = null)
        {
            ShowMessage(Properties.Resources.DefaultUIFactory_ErrorTitle, Properties.Resources.DefaultUIFactory_ShowCannotDownloadAppcastMessage, applicationIcon);
        }

        /// <summary>
        /// Show 'toast' window to notify new version is available
        /// </summary>
        /// <param name="updates">Appcast updates</param>
        /// <param name="applicationIcon">Icon to use in window</param>
        /// <param name="clickHandler">handler for click</param>
        public virtual void ShowToast(List<AppCastItem> updates, Uri applicationIcon, Action<List<AppCastItem>> clickHandler)
        {
            //var toast = new ToastNotifier
            //    {
            //        Image =
            //            {
            //                Image = applicationIcon != null ? BitmapFrame.Create(applicationIcon) : Properties.Resources.software_update_available
            //            }
            //    };
            //toast.ToastClicked += (sender, args) => clickHandler(updates); // TODO: this is leak
            //toast.Show(Properties.Resources.DefaultUIFactory_ToastMessage, Properties.Resources.DefaultUIFactory_ToastCallToAction, 5);
        }

        /// <summary>
        /// Show message on download error
        /// </summary>
        /// <param name="message">Error message from exception</param>
        /// <param name="appcastUrl">the URL for the appcast file</param>
        /// <param name="applicationIcon">The icon to display</param>
        public virtual void ShowDownloadErrorMessage(string message, string appcastUrl, Uri applicationIcon = null)
        {
            ShowMessage(Properties.Resources.DefaultUIFactory_ErrorTitle, string.Format(Properties.Resources.DefaultUIFactory_ShowDownloadErrorMessage, message), applicationIcon);
        }

        private void ShowMessage(string title, string message, Uri applicationIcon = null)
        {
            var messageWindow = new MessageNotificationWindow(title, message, applicationIcon) { Owner = ParentWindow };
            messageWindow.ShowDialog();
        }
    }
}
