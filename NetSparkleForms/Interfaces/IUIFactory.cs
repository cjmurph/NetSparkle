using NetSparkle;
using NetSparkle.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetSparkleForms.Interfaces
{
    /// <summary>
    /// Universal interface for creating UI utilized by Sparkle
    /// </summary>
    public interface IFormsFactory
    {
        /// <summary>
        /// Parent window for pop ups
        /// </summary>
        System.Windows.Forms.Form ParentWindow { get; set; }

        /// <summary>
        /// Create sparkle form implementation. This is the form that tells the user that an update is available, shows changelogs if necessary, etc.
        /// </summary>
        /// <param name="sparkle">The <see cref="Sparkle"/> instance to use</param>
        /// <param name="updates">Sorted array of updates from latest to previous</param>
        /// <param name="applicationIcon">Icon</param>
        /// <param name="isUpdateAlreadyDownloaded">If true, make sure UI text shows that the user is about to install the file instead of download it.</param>
        IUpdateAvailable CreateSparkleForm(Sparkle sparkle, List<AppCastItem> updates, Icon applicationIcon, bool isUpdateAlreadyDownloaded = false);

        /// <summary>
        /// Create download progress window
        /// </summary>
        /// <param name="item">Appcast item to download</param>
        /// <param name="applicationIcon">Application icon to use</param>
        IDownloadProgress CreateProgressWindow(AppCastItem item, Icon applicationIcon);

        /// <summary>
        /// Inform user in some way that NetSparkle is checking for updates
        /// </summary>
        /// <param name="applicationIcon">The icon to display</param>
        ICheckingForUpdates ShowCheckingForUpdates(Icon applicationIcon = null);

        /// <summary>
        /// Initialize UI. Called when Sparkle is constructed.
        /// </summary>
        void Init();

        /// <summary>
        /// Show user a message saying downloaded update format is unknown
        /// </summary>
        void ShowUnknownInstallerFormatMessage(string downloadFileName, Icon applicationIcon = null);

        /// <summary>
        /// Show user that current installed version is up-to-date
        /// </summary>
        void ShowVersionIsUpToDate(Icon applicationIcon = null);

        /// <summary>
        /// Show message that latest update was skipped by user
        /// </summary>
        void ShowVersionIsSkippedByUserRequest(Icon applicationIcon = null);

        /// <summary>
        /// Show message that appcast is not available
        /// </summary>
        void ShowCannotDownloadAppcast(string appcastUrl, Icon applicationIcon = null);

        /// <summary>
        /// Show 'toast' window to notify new version is available
        /// </summary>
        /// <param name="updates">Appcast updates</param>
        /// <param name="applicationIcon">Icon to use in window</param>
        /// <param name="clickHandler">handler for click</param>
        void ShowToast(List<AppCastItem> updates, Icon applicationIcon, Action<List<AppCastItem>> clickHandler);

        /// <summary>
        /// Show message on download error
        /// </summary>
        /// <param name="message">Error message from exception</param>
        /// <param name="appcastUrl">the URL for the appcast file</param>
        /// <param name="applicationIcon">Icon to use in window</param>
        void ShowDownloadErrorMessage(string message, string appcastUrl, Icon applicationIcon = null);
    }
}
