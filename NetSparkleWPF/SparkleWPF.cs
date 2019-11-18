using System;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Threading;
using NetSparkle.Interfaces;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using NetSparkle.Enums;
using NetSparkle;
using NetSparkleWPF.Interfaces;
using NetSparkleWPF.Windows;
using System.Windows;
using System.Windows.Interop;

// TODO: resume downloads if the download didn't finish but the software was killed
// instead of restarting the entire download
// TODO: Refactor a bunch of events to an interface instead?
// TODO: That loop thing for the background worker needs to be reworked to have no goto and such.

namespace NetSparkleWPF
{
    /// <summary>
    /// Windows forms default UI wrapper for NetSparkle
    /// </summary>
    public class SparkleWPF : IDisposable
    {
        /// <summary>
        /// Subscribe to this to get a chance to shut down gracefully before quitting.
        /// If <see cref="AboutToExitForInstallerRunAsync"/> is set, this has no effect.
        /// </summary>
        public event CancelEventHandler AboutToExitForInstallerRun;

        /// <summary>
        /// Subscribe to this to get a chance to asynchronously shut down gracefully before quitting.
        /// This overrides <see cref="AboutToExitForInstallerRun"/>.
        /// </summary>
        public event CancelEventHandlerAsync AboutToExitForInstallerRunAsync;

        /// <summary>
        /// This event will be raised when a check loop will be started
        /// </summary>
        public event LoopStartedOperation CheckLoopStarted;

        /// <summary>
        /// This event will be raised when a check loop is finished
        /// </summary>
        public event LoopFinishedOperation CheckLoopFinished;

        /// <summary>
        /// This event can be used to override the standard user interface
        /// process when an update is detected
        /// </summary>
        public event UpdateDetected UpdateDetected;

        /// <summary>
        /// Event for custom shutdown logic. If this is set, it is called instead of
        /// Application.Current.Shutdown or Application.Exit.
        /// If <see cref="CloseApplicationAsync"/> is set, this has no effect.
        /// <para>Warning: The batch file that launches your executable only waits for 90 seconds before
        /// giving up! Make sure that your software closes within 90 seconds if you implement this event!
        /// If you need an event that can be canceled, use <see cref="AboutToExitForInstallerRun"/>.</para>
        /// </summary>
        public event CloseApplication CloseApplication;

        /// <summary>
        /// Event for asynchronous custom shutdown logic. If this is set, it is called instead of
        /// Application.Current.Shutdown or Application.Exit.
        /// This overrides <see cref="CloseApplication"/>.
        /// <para>Warning: The batch file that launches your executable only waits for 90 seconds before
        /// giving up! Make sure that your software closes within 90 seconds if you implement this event!
        /// If you need an event that can be canceled, use <see cref="AboutToExitForInstallerRunAsync"/>.</para>
        /// </summary>
        public event CloseApplicationAsync CloseApplicationAsync;

        /// <summary>
        /// Called when update check has just started
        /// </summary>
        public event UpdateCheckStarted UpdateCheckStarted;

        /// <summary>
        /// Called when update check is all done. May or may not have called <see cref="UpdateDetected"/> in the middle.
        /// </summary>
        public event UpdateCheckFinished UpdateCheckFinished;

        /// <summary>
        /// Called when the downloaded file is fully downloaded and verified regardless of the value for
        /// SilentMode. Note that if you are installing fully silently, this will be called before the
        /// install file is executed, so don't manually initiate the file or anything.
        /// </summary>
        public event DownloadedFileReady DownloadedFileReady;

        /// <summary>
        /// Called when the downloaded file is downloaded (or at least partially on disk) and the DSA
        /// signature doesn't match. When this is called, Sparkle is not taking any further action to
        /// try to download the install file during this instance of the software. In order to make Sparkle
        /// try again, you must delete the file off disk yourself. Sparkle will try again after the software
        /// is restarted.
        /// </summary>
        public event DownloadedFileIsCorrupt DownloadedFileIsCorrupt;

        /// <summary>
        /// Called when the user skips some version of the application.
        /// </summary>
        public event UserSkippedVersion UserSkippedVersion;
        /// <summary>
        /// Called when the user skips some version of the application by clicking
        /// the 'Remind Me Later' button.
        /// </summary>
        public event RemindMeLaterSelected RemindMeLaterSelected;
        /// <summary>
        /// Download will commence, create dialogues
        /// </summary>
        public event DownloadInitialize InitializeDownloading;
        /// <summary>
        /// Called when the download has just started
        /// </summary>
        public event DownloadEvent StartedDownloading;
        /// <summary>
        /// Called when the download progress changes
        /// </summary>
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        /// <summary>
        /// Called when the download has finished successfully
        /// </summary>
        public event DownloadEvent FinishedDownloading;
        /// <summary>
        /// Called when the download has been canceled
        /// </summary>
        public event DownloadEvent DownloadCanceled;
        /// <summary>
        /// Called when the download has downloaded but has an error other than corruption
        /// </summary>
        public event DownloadEvent DownloadError;

        public static readonly Uri DefaultIcon = new Uri("pack://application:,,,/NetSparkleWPF;component/Resources/default.ico");

        private SynchronizationContext _syncContext;

        private readonly Uri _applicationIcon;
        private bool _useNotificationToast;
        private bool _disposed;

        /// <summary>
        /// NetSparkle Instance
        /// </summary>
        public Sparkle Sparkle { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SparkleWPF"/> class with the given appcast URL.
        /// </summary>
        /// <param name="appcastUrl">the URL of the appcast file</param>
        public SparkleWPF(string appcastUrl)
            : this(appcastUrl, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SparkleWPF"/> class with the given appcast URL
        /// and an <see cref="Icon"/> for the update UI.
        /// </summary>
        /// <param name="appcastUrl">the URL of the appcast file</param>
        /// <param name="applicationIcon"><see cref="Icon"/> to be displayed in the update UI.
        /// If you're invoking this from a form, this would be <c>this.Icon</c>.</param>
        public SparkleWPF(string appcastUrl, Uri applicationIcon)
            : this(appcastUrl, applicationIcon, SecurityMode.Strict, null)
        { }

        /// <summary>
        /// ctor which needs the appcast url
        /// </summary>
        /// <param name="appcastUrl">the URL of the appcast file</param>
        /// <param name="applicationIcon"><see cref="Icon"/> to be displayed in the update UI.
        /// If invoking this from a form, this would be <c>this.Icon</c>.</param>
        /// <param name="securityMode">the security mode to be used when checking DSA signatures</param>
        public SparkleWPF(string appcastUrl, Uri applicationIcon, SecurityMode securityMode)
            : this(appcastUrl, applicationIcon, securityMode, null)
        { }

        /// <summary>
        /// ctor which needs the appcast url
        /// </summary>
        /// <param name="appcastUrl">the URL of the appcast file</param>
        /// <param name="applicationIcon"><see cref="Icon"/> to be displayed in the update UI.
        /// If invoking this from a form, this would be <c>this.Icon</c>.</param>
        /// <param name="securityMode">the security mode to be used when checking DSA signatures</param>
        /// <param name="dsaPublicKey">the DSA public key for checking signatures, in XML Signature (&lt;DSAKeyValue&gt;) format.
        /// If null, a file named "NetSparkle_DSA.pub" is used instead.</param>
        public SparkleWPF(string appcastUrl, Uri applicationIcon, SecurityMode securityMode, string dsaPublicKey)
            : this(appcastUrl, applicationIcon, securityMode, dsaPublicKey, null)
        { }

        /// <summary>
        /// ctor which needs the appcast url and a referenceassembly
        /// </summary>        
        /// <param name="appcastUrl">the URL of the appcast file</param>
        /// <param name="applicationIcon"><see cref="Icon"/> to be displayed in the update UI.
        /// If invoking this from a form, this would be <c>this.Icon</c>.</param>
        /// <param name="securityMode">the security mode to be used when checking DSA signatures</param>
        /// <param name="dsaPublicKey">the DSA public key for checking signatures, in XML Signature (&lt;DSAKeyValue&gt;) format.
        /// If null, a file named "NetSparkle_DSA.pub" is used instead.</param>
        /// <param name="referenceAssembly">the name of the assembly to use for comparison when checking update versions</param>
        public SparkleWPF(string appcastUrl, Uri applicationIcon, SecurityMode securityMode, string dsaPublicKey, string referenceAssembly)
            : this(appcastUrl, applicationIcon, securityMode, dsaPublicKey, referenceAssembly, new DefaultUIFactory())
        { }

        /// <summary>
        /// ctor which needs the appcast url and a referenceassembly
        /// </summary>        
        /// <param name="appcastUrl">the URL of the appcast file</param>
        /// <param name="applicationIcon"><see cref="Icon"/> to be displayed in the update UI.
        /// If invoking this from a form, this would be <c>this.Icon</c>.</param>
        /// <param name="securityMode">the security mode to be used when checking DSA signatures</param>
        /// <param name="dsaPublicKey">the DSA public key for checking signatures, in XML Signature (&lt;DSAKeyValue&gt;) format.
        /// If null, a file named "NetSparkle_DSA.pub" is used instead.</param>
        /// <param name="referenceAssembly">the name of the assembly to use for comparison when checking update versions</param>
        /// <param name="factory">a UI factory to use in place of the default UI</param>
        public SparkleWPF(string appcastUrl, Uri applicationIcon, SecurityMode securityMode, string dsaPublicKey, string referenceAssembly, IWpfFactory factory)
        {
            Sparkle = new Sparkle(appcastUrl, securityMode, dsaPublicKey, referenceAssembly);

            Sparkle.AboutToExitForInstallerRun += Sparkle_AboutToExitForInstallerRun;
            Sparkle.AboutToExitForInstallerRunAsync += Sparkle_AboutToExitForInstallerRunAsync;
            Sparkle.CheckLoopFinished += Sparkle_CheckLoopFinished;
            Sparkle.CheckLoopStarted += Sparkle_CheckLoopStarted;
            Sparkle.CloseApplication += Sparkle_CloseApplication;
            Sparkle.CloseApplicationAsync += Sparkle_CloseApplicationAsync;
            Sparkle.DownloadCanceled += Sparkle_DownloadCanceled;
            Sparkle.DownloadedFileIsCorrupt += Sparkle_DownloadedFileIsCorrupt;
            Sparkle.DownloadedFileReady += Sparkle_DownloadedFileReady;
            Sparkle.DownloadError += Sparkle_DownloadError;
            Sparkle.DownloadProgressChanged += Sparkle_DownloadProgressChanged;
            Sparkle.FinishedDownloading += Sparkle_FinishedDownloading;
            Sparkle.RemindMeLaterSelected += Sparkle_RemindMeLaterSelected;
            Sparkle.InitializeDownloading += Sparkle_InitializeDownloading;
            Sparkle.StartedDownloading += Sparkle_StartedDownloading;
            Sparkle.UpdateCheckFinished += Sparkle_UpdateCheckFinished;
            Sparkle.UpdateCheckStarted += Sparkle_UpdateCheckStarted;
            Sparkle.UpdateDetected += Sparkle_UpdateDetected;
            Sparkle.UserSkippedVersion += Sparkle_UserSkippedVersion;

            _applicationIcon = applicationIcon ?? DefaultIcon;

            UIFactory = factory;

            // Syncronisation Context
            _syncContext = SynchronizationContext.Current;
            if (_syncContext == null)
            {
                _syncContext = new SynchronizationContext();
            }

            UIFactory.Init();

            HideSkipButton = false;
            HideRemindMeLaterButton = false;
        }

        /// <summary>
        /// The security protocol used by NetSparkle. Setting this property will also set this 
        /// for the current AppDomain of the caller. Needs to be set to 
        /// SecurityProtocolType.Tls12 for some cases.
        /// </summary>
        public SecurityProtocolType SecurityProtocolType
        {
            get => Sparkle.SecurityProtocolType;
            set { Sparkle.SecurityProtocolType = value; }
        }

        private void Sparkle_UserSkippedVersion(AppCastItem item, string downloadPath)
        {
            UserSkippedVersion?.Invoke(item, downloadPath);
        }

        private void Sparkle_UpdateDetected(object sender, UpdateDetectedEventArgs e)
        {
            UpdateDetected?.Invoke(this, e);
            if (Sparkle.IsQuietCheck) return;
            switch (e.NextAction)
            {
                case NextUpdateAction.ShowStandardUserInterface:
                    ShowUpdateNeededUI(e.AppCastItems);
                    break;
                case NextUpdateAction.PerformUpdateUnattended:
                    Sparkle.SilentMode = SilentModeTypes.DownloadAndInstall;
                    Sparkle.RelaunchAfterUpdate = true;
                    Sparkle.RespondToUpdateAvailable(e.AppCastItems.FirstOrDefault(), UpdateAvailableResult.InstallUpdate);
                    break;
                case NextUpdateAction.ProhibitUpdate:
                    break;
                default:
                    break;
            }

        }

        private void Sparkle_UpdateCheckStarted(object sender)
        {
            UpdateCheckStarted?.Invoke(this);

            if (Sparkle.IsQuietCheck) return;

            if (CheckingForUpdatesWindow == null)
                CheckingForUpdatesWindow = UIFactory.ShowCheckingForUpdates(_applicationIcon);
            CheckingForUpdatesWindow.Show();
        }

        private void Sparkle_UpdateCheckFinished(object sender, UpdateStatus status)
        {
            UpdateCheckFinished?.Invoke(this, status);
            CheckingForUpdatesWindow.Close();
            CheckingForUpdatesWindow = null;
        }

        private async Task Sparkle_InitializeDownloading()
        {
            if (InitializeDownloading != null)
                await InitializeDownloading.Invoke();
            await InitializeProgressWindow(Sparkle.LatestAppCastItems.FirstOrDefault());
        }

        private void Sparkle_StartedDownloading(string path)
        {
            StartedDownloading?.Invoke(path);
            ShowProgressWindow();
        }

        private void Sparkle_RemindMeLaterSelected(AppCastItem item)
        {
            RemindMeLaterSelected?.Invoke(item);
        }

        private void Sparkle_FinishedDownloading(string path)
        {
            FinishedDownloading?.Invoke(path);
            ProgressWindow?.FinishedDownloadingFile(true);
        }

        private void Sparkle_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgressChanged?.Invoke(this, e);
            //update progress on download form
            ProgressWindow?.OnDownloadProgressChanged(this, e);
        }

        private void Sparkle_DownloadError(string path)
        {
            DownloadError?.Invoke(path);
            if (ProgressWindow != null && !ProgressWindow.DisplayErrorMessage("Download Error"))
            {
                UIFactory.ShowDownloadErrorMessage("Download Error", Sparkle.AppcastUrl, _applicationIcon);
            }
        }

        private void Sparkle_DownloadedFileReady(AppCastItem item, string downloadPath)
        {
            DownloadedFileReady?.Invoke(item, downloadPath);
        }

        private void Sparkle_DownloadedFileIsCorrupt(AppCastItem item, string downloadPath)
        {
            DownloadedFileIsCorrupt?.Invoke(item, downloadPath);
            if (ProgressWindow != null && !ProgressWindow.DisplayErrorMessage("Downloaded file has invalid signature!"))
            {
                UIFactory.ShowDownloadErrorMessage("Downloaded file has invalid signature!", Sparkle.AppcastUrl, _applicationIcon);
            }
        }

        private void Sparkle_DownloadCanceled(string path)
        {
            DownloadCanceled?.Invoke(path);
            if (ProgressWindow != null && !ProgressWindow.DisplayErrorMessage("Download Cancelled"))
            {
                UIFactory.ShowDownloadErrorMessage("Download Cancelled", Sparkle.AppcastUrl, _applicationIcon);
            }
        }

        private Task Sparkle_CloseApplicationAsync()
        {
            return CloseApplicationAsync?.Invoke() ?? Task.FromResult(0);
        }

        private void Sparkle_CloseApplication()
        {
            CloseApplication?.Invoke();
            Application.Current.Shutdown();
        }

        private void Sparkle_CheckLoopStarted(object sender)
        {
            CheckLoopStarted?.Invoke(this);
        }

        private void Sparkle_CheckLoopFinished(object sender, bool updateRequired)
        {
            CheckLoopFinished?.Invoke(this, updateRequired);
        }

        private Task Sparkle_AboutToExitForInstallerRunAsync(object sender, CancelEventArgs e)
        {
            return AboutToExitForInstallerRunAsync?.Invoke(this, e) ?? Task.FromResult(0);
        }

        private void Sparkle_AboutToExitForInstallerRun(object sender, CancelEventArgs e)
        {
            AboutToExitForInstallerRun?.Invoke(this, e);
        }

        /// <summary>
        /// (WinForms only) Schedules an update check to happen on the first Application.Idle event.
        /// </summary>
        public void CheckOnFirstApplicationIdle()
        {
            ComponentDispatcher.ThreadIdle += OnFirstApplicationIdle;
        }

        private async void OnFirstApplicationIdle(object sender, EventArgs e)
        {
            ComponentDispatcher.ThreadIdle -= OnFirstApplicationIdle;
            await Sparkle.CheckForUpdates();
        }


        #region Properties

        /// <summary>
        /// Hides the release notes view when an update is found.
        /// </summary>
        public bool HideReleaseNotes { get; set; }

        /// <summary>
        /// Hides the skip this update button when an update is found.
        /// </summary>
        public bool HideSkipButton { get; set; }

        /// <summary>
        /// Hides the remind me later button when an update is found.
        /// </summary>
        public bool HideRemindMeLaterButton { get; set; }

        /// <summary>
        /// Set the silent mode type for Sparkle to use when there is a valid update for the software
        /// </summary>
        public SilentModeTypes SilentMode
        {
            get => Sparkle.SilentMode;
            set
            {
                Sparkle.SilentMode = value;
            }
        }

        public Window ParentWindow { get => UIFactory.ParentWindow; set => UIFactory.ParentWindow = value; }

        /// <summary>
        /// If set, downloads files to this path. If the folder doesn't already exist, creates
        /// the folder at download time (and not before). 
        /// Note that this variable is a path, not a full file name.
        /// </summary>
        public string TmpDownloadFilePath
        {
            get => Sparkle.TmpDownloadFilePath;
            set
            {
                Sparkle.TmpDownloadFilePath = value?.Trim();
            }
        }

        /// <summary>
        /// Defines if the application needs to be relaunched after executing the downloaded installer
        /// </summary>
        public bool RelaunchAfterUpdate { get => Sparkle.RelaunchAfterUpdate; set { Sparkle.RelaunchAfterUpdate = value; } }

        /// <summary>
        /// Run the downloaded installer with these arguments
        /// </summary>
        public string CustomInstallerArguments { get => Sparkle.CustomInstallerArguments; set { Sparkle.CustomInstallerArguments = value; } }

        /// <summary>
        /// Function that is called asynchronously to clean up old installers that have been
        /// downloaded with SilentModeTypes.DownloadNoInstall or SilentModeTypes.DownloadAndInstall.
        /// </summary>
        public Action ClearOldInstallers { get => Sparkle.ClearOldInstallers; set { Sparkle.ClearOldInstallers = value; } }

        /// <summary>
        /// Whether or not the update loop is running
        /// </summary>
        public bool IsUpdateLoopRunning => Sparkle.IsUpdateLoopRunning;

        /// <summary>
        /// If true, don't check the validity of SSL certificates
        /// </summary>
        public bool TrustEverySSLConnection { get => Sparkle.TrustEverySSLConnection; set { Sparkle.TrustEverySSLConnection = value; } }

        /// <summary>
        /// Factory for creating UI forms like progress window, etc.
        /// </summary>
        public IWpfFactory UIFactory { get; set; }

        /// <summary>
        /// The user interface window that shows the release notes and
        /// asks the user to skip, remind me later, or update
        /// </summary>
        public IUpdateAvailable UserWindow { get; set; }

        /// <summary>
        /// The user interface window that shows a download progress bar,
        /// and then asks to install and relaunch the application
        /// </summary>
        public IDownloadProgress ProgressWindow { get; set; }

        /// <summary>
        /// The user interface window that shows the 'Checking for Updates...'
        /// form. TODO: Make this an interface so user can config their own UI
        /// </summary>
        public ICheckingForUpdates CheckingForUpdatesWindow { get; set; }

        /// <summary>
        /// The NetSparkle configuration object for the current assembly.
        /// </summary>
        public Configuration Configuration { get => Sparkle.Configuration; set { Sparkle.Configuration = value; } }

        /// <summary>
        /// The DSA checker
        /// </summary>
        public DSAChecker DSAChecker { get => Sparkle.DSAChecker; set { Sparkle.DSAChecker = value; } }

        /// <summary>
        /// Gets or sets the appcast URL
        /// </summary>
        public string AppcastUrl
        {
            get => Sparkle.AppcastUrl;
            set { Sparkle.AppcastUrl = value; }
        }

        /// <summary>
        /// Specifies if you want to use the notification toast
        /// </summary>
        public bool UseNotificationToast
        {
            get { return _useNotificationToast; }
            set { _useNotificationToast = value; }
        }

        /// <summary>
        /// WinForms only. If true, tries to run UI code on the main thread using <see cref="SynchronizationContext"/>.
        /// </summary>
        public bool ShowsUIOnMainThread { get; set; }

        /// <summary>
        /// If not "", sends extra JSON via POST to server with the web request for update information and for the DSA signature.
        /// </summary>
        public string ExtraJsonData { get => Sparkle.ExtraJsonData; set { Sparkle.ExtraJsonData = value; } }

        /// <summary>
        /// Object that handles any diagnostic messages for NetSparkle.
        /// If you want to use your own class for this, you should just
        /// need to override <see cref="LogWriter.PrintMessage"/> in your own class.
        /// Make sure to set this object before calling <see cref="StartLoop(bool)"/> to guarantee
        /// that all messages will get sent to the right place!
        /// </summary>
        public LogWriter LogWriter => Sparkle.LogWriter;

        public string DownloadProgressMessage => Sparkle.DownloadProgressMessage;

        /// <summary>
        /// Returns the latest appcast items to the caller. Might be null.
        /// </summary>
        public List<AppCastItem> LatestAppCastItems => Sparkle.LatestAppCastItems;

        /// <summary>
        /// Loops through all of the most recently grabbed app cast items
        /// and checks if any of them are marked as critical
        /// </summary>
        public bool UpdateMarkedCritical => Sparkle.UpdateMarkedCritical;

        #endregion

        /// <summary>
        /// Starts a NetSparkle background loop to check for updates every 24 hours.
        /// <para>You should only call this function when your app is initialized and shows its main window.</para>
        /// </summary>
        /// <param name="doInitialCheck">whether the first check should happen before or after the first interval</param>
        public void StartLoop(bool doInitialCheck)
        {
            Sparkle.StartLoop(doInitialCheck);
        }

        /// <summary>
        /// Starts a NetSparkle background loop to check for updates on a given interval.
        /// <para>You should only call this function when your app is initialized and shows its main window.</para>
        /// </summary>
        /// <param name="doInitialCheck">whether the first check should happen before or after the first interval</param>
        /// <param name="checkFrequency">the interval to wait between update checks</param>
        public void StartLoop(bool doInitialCheck, TimeSpan checkFrequency)
        {
            Sparkle.StartLoop(doInitialCheck, checkFrequency);
        }

        /// <summary>
        /// Starts a NetSparkle background loop to check for updates every 24 hours.
        /// <para>You should only call this function when your app is initialized and shows its main window.</para>
        /// </summary>
        /// <param name="doInitialCheck">whether the first check should happen before or after the first interval</param>
        /// <param name="forceInitialCheck">if <paramref name="doInitialCheck"/> is true, whether the first check
        /// should happen even if the last check was less than 24 hours ago</param>
        public void StartLoop(bool doInitialCheck, bool forceInitialCheck)
        {
            Sparkle.StartLoop(doInitialCheck, forceInitialCheck);
        }

        /// <summary>
        /// Starts a NetSparkle background loop to check for updates on a given interval.
        /// <para>You should only call this function when your app is initialized and shows its main window.</para>
        /// </summary>
        /// <param name="doInitialCheck">whether the first check should happen before or after the first period</param>
        /// <param name="forceInitialCheck">if <paramref name="doInitialCheck"/> is true, whether the first check
        /// should happen even if the last check was within the last <paramref name="checkFrequency"/> interval</param>
        /// <param name="checkFrequency">the interval to wait between update checks</param>
        public void StartLoop(bool doInitialCheck, bool forceInitialCheck, TimeSpan checkFrequency)
        {
            Sparkle.StartLoop(doInitialCheck, forceInitialCheck, checkFrequency);
        }

        /// <summary>
        /// Stops the Sparkle background loop./>.
        /// </summary>
        public void StopLoop()
        {
            Sparkle.StopLoop();
        }

        /// <summary>
        /// Inherited from IDisposable. Stops all background activities.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnregisterEvents();
                    Sparkle.Dispose();
                }
                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            _disposed = true;


        }

        /// <summary>
        /// Unregisters events so that we don't have multiple items updating
        /// </summary>
        private void UnregisterEvents()
        {
            if (UserWindow != null)
            {
                UserWindow.UserResponded -= OnUserWindowUserResponded;
                UserWindow = null;
            }

            if (ProgressWindow != null)
            {
                WeakEventManager<IDownloadProgress, NetSparkle.Events.DownloadInstallArgs>.RemoveHandler(ProgressWindow, nameof(ProgressWindow.DownloadProcessCompleted), OnProgressWindowDownloadProcessComplete);
                ProgressWindow = null;
            }

            if (Sparkle != null)
            {
                Sparkle.AboutToExitForInstallerRun -= Sparkle_AboutToExitForInstallerRun;
                Sparkle.AboutToExitForInstallerRunAsync -= Sparkle_AboutToExitForInstallerRunAsync;
                Sparkle.CheckLoopFinished -= Sparkle_CheckLoopFinished;
                Sparkle.CheckLoopStarted -= Sparkle_CheckLoopStarted;
                Sparkle.CloseApplication -= Sparkle_CloseApplication;
                Sparkle.CloseApplicationAsync -= Sparkle_CloseApplicationAsync;
                Sparkle.DownloadCanceled -= Sparkle_DownloadCanceled;
                Sparkle.DownloadedFileIsCorrupt -= Sparkle_DownloadedFileIsCorrupt;
                Sparkle.DownloadedFileReady -= Sparkle_DownloadedFileReady;
                Sparkle.DownloadError -= Sparkle_DownloadError;
                Sparkle.DownloadProgressChanged -= Sparkle_DownloadProgressChanged;
                Sparkle.FinishedDownloading -= Sparkle_FinishedDownloading;
                Sparkle.RemindMeLaterSelected -= Sparkle_RemindMeLaterSelected;
                Sparkle.InitializeDownloading -= Sparkle_InitializeDownloading;
                Sparkle.StartedDownloading -= Sparkle_StartedDownloading;
                Sparkle.UpdateCheckFinished -= Sparkle_UpdateCheckFinished;
                Sparkle.UpdateCheckStarted -= Sparkle_UpdateCheckStarted;
                Sparkle.UpdateDetected -= Sparkle_UpdateDetected;
                Sparkle.UserSkippedVersion -= Sparkle_UserSkippedVersion;
            }
        }

        /// <summary>
        /// This method checks if an update is required. During this process the appcast
        /// will be downloaded and checked against the reference assembly. Ensure that
        /// the calling process has read access to the reference assembly.
        /// This method is also called from the background loops.
        /// </summary>
        /// <param name="config">the NetSparkle configuration for the reference assembly</param>
        /// <returns><see cref="UpdateInfo"/> with information on whether there is an update available or not.</returns>
        public async Task<UpdateInfo> GetUpdateStatus(Configuration config)
        {
            return await Sparkle.GetUpdateStatus(config);
        }

        /// <summary>
        /// Reads the local Sparkle configuration for the given reference assembly.
        /// </summary>
        public Configuration GetApplicationConfig()
        {
            return Sparkle.GetApplicationConfig();
        }

        /// <summary>
        /// Shows the update needed UI with the given set of updates.
        /// </summary>
        /// <param name="updates">updates to show UI for</param>
        /// <param name="isUpdateAlreadyDownloaded">If true, make sure UI text shows that the user is about to install the file instead of download it.</param>
        public void ShowUpdateNeededUI(List<AppCastItem> updates, bool isUpdateAlreadyDownloaded = false)
        {
            if (updates != null)
            {
                if (_useNotificationToast)
                {
                    UIFactory.ShowToast(updates, _applicationIcon, OnToastClick);
                }
                else
                {
                    ShowUpdateNeededUIInner(updates, isUpdateAlreadyDownloaded);
                }
            }
        }

        /// <summary>
        /// Shows the update UI with the latest downloaded update information.
        /// </summary>
        /// <param name="isUpdateAlreadyDownloaded">If true, make sure UI text shows that the user is about to install the file instead of download it.</param>
        public void ShowUpdateNeededUI(bool isUpdateAlreadyDownloaded = false)
        {
            ShowUpdateNeededUI(LatestAppCastItems, isUpdateAlreadyDownloaded);
        }

        private void OnToastClick(List<AppCastItem> updates)
        {
            ShowUpdateNeededUIInner(updates);
        }

        private async void ShowUpdateNeededUIInner(List<AppCastItem> updates, bool isUpdateAlreadyDownloaded = false)
        {
            // create the form
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (UserWindow != null)
                        UserWindow.Close();

                    UserWindow = UIFactory.CreateSparkleForm(Sparkle, updates, _applicationIcon, isUpdateAlreadyDownloaded);

                    if (HideReleaseNotes)
                    {
                        UserWindow.HideReleaseNotes();
                    }
                    if (HideSkipButton)
                    {
                        UserWindow.HideSkipButton();
                    }
                    if (HideRemindMeLaterButton)
                    {
                        UserWindow.HideRemindMeLaterButton();
                    }

                    // clear if already set.
                    UserWindow.UserResponded += OnUserWindowUserResponded;
                    UserWindow.Show();
                }
                catch(Exception e)
                {
                    LogWriter.PrintMessage("Error showing sparkle form: {0}", e.Message);
                }
            }));
        }

        /// <summary>
        /// Get the download path for a given app cast item.
        /// If any directories need to be created, this function
        /// will create those directories.
        /// </summary>
        /// <param name="item">The item that you want to generate a download path for</param>
        /// <returns>The download path for an app cast item if item is not null and has valid download link
        /// Otherwise returns null.</returns>
        public string DownloadPathForAppCastItem(AppCastItem item)
        {
            return Sparkle.DownloadPathForAppCastItem(item);
        }

        private async Task InitializeProgressWindow(AppCastItem castItem)
        {
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ProgressWindow != null)
                {
                    WeakEventManager<IDownloadProgress, NetSparkle.Events.DownloadInstallArgs>.RemoveHandler(ProgressWindow, nameof(ProgressWindow.DownloadProcessCompleted), OnProgressWindowDownloadProcessComplete);
                    ProgressWindow.Close();
                    ProgressWindow = null;
                }
                if (ProgressWindow == null && !IsDownloadingSilently)
                {
                    ProgressWindow = UIFactory.CreateProgressWindow(castItem, _applicationIcon);
                    WeakEventManager<IDownloadProgress, NetSparkle.Events.DownloadInstallArgs>.AddHandler(ProgressWindow, nameof(ProgressWindow.DownloadProcessCompleted), OnProgressWindowDownloadProcessComplete);
                }
            }));         
        }

        /// <summary>
        /// Shows the progress window if not downloading silently.
        /// </summary>
        private void ShowProgressWindow()
        {
            if (!IsDownloadingSilently && ProgressWindow != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!ProgressWindow.ShowDialog())
                    {
                        CancelFileDownload();
                    }
                }));
            }
        }

        /// <summary>
        /// True if the user has silent updates enabled; false otherwise.
        /// </summary>
        private bool IsDownloadingSilently => Sparkle.IsDownloadingSilently;



        /// <summary>
        /// Quits the application (host application) 
        /// </summary>
        /// <returns>Runs asynchrously, so returns a Task</returns>
        public async Task QuitApplication()
        {
            await Sparkle.QuitApplication();
        }

        /// <summary>
        /// Check for updates, using interaction appropriate for if the user just said "check for updates".
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdatesAtUserRequest()
        {
            var result = await Sparkle.CheckForUpdatesAtUserRequest();
            return result;
        }

        /// <summary>
        /// Check for updates, using interaction appropriate for where the user doesn't know you're doing it, so be polite.
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdatesQuietly()
        {
            return await Sparkle.CheckForUpdatesQuietly();
        }

        /// <summary>
        /// Check for updates, using interaction appropriate for where the user doesn't know you're doing it, so be polite.
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdates()
        {
            return await Sparkle.CheckForUpdates();
        }

        /// <summary>
        /// Cancels an in-progress download and deletes the temporary file.
        /// </summary>
        public void CancelFileDownload()
        {
            Sparkle.CancelFileDownload();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        private void OnUserWindowUserResponded(object sender, EventArgs e)
        {
            Sparkle.RespondToUpdateAvailable(UserWindow.CurrentItem, UserWindow.Result);

            UserWindow.Close();
            UserWindow = null; // done using the window so don't hold onto reference
            CheckingForUpdatesWindow?.Close();
            CheckingForUpdatesWindow = null;
        }

        /// <summary>
        /// Called when the progress bar fires the update event
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        private void OnProgressWindowDownloadProcessComplete(object sender, NetSparkle.Events.DownloadInstallArgs e)
        {
            if (!e.ShouldInstall) return;
            //TODO, handle this in the fired events
            ProgressWindow?.SetDownloadAndInstallButtonEnabled(false); // disable while we ask if we can close up the software
            Sparkle.InstallAndRelaunch();
            ProgressWindow?.SetDownloadAndInstallButtonEnabled(true);

        }



        /// <summary>
        /// Called when a Windows forms application exits. Starts the installer.
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="e">not used.</param>
        private void OnApplicationExit(object sender, EventArgs e)
        {
            Application.Current.Exit -= OnApplicationExit;
        }
    }
}