using NetSparkle;
using NetSparkle.Enums;
using NetSparkleWPF.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetSparkleWPF.Windows
{
    public class UpdateAvailableViewModel:ObservableObject
    {
        private string _header;

        public string Header
        {
            get => _header;
            set
            {
                if (_header == value) return;
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }

        private string _info;

        public string Info
        {
            get => _info;
            set
            {
                if (_info == value) return;
                _info = value;
                OnPropertyChanged(nameof(Info));
            }
        }

        private string _releaseNotes;

        public string ReleaseNotes
        {
            get => _releaseNotes;
            set
            {
                if (_releaseNotes == value) return;
                _releaseNotes = value;
                OnPropertyChanged(nameof(ReleaseNotes));
            }
        }

        public ImageSource Icon
        {
            get { return BitmapFrame.Create(IconUri); }
        }

        private Uri _iconUri;

        public Uri IconUri
        {
            get => _iconUri;
            set
            {
                if (_iconUri == value) return;
                _iconUri = value;
                OnPropertyChanged(nameof(IconUri));
                OnPropertyChanged(nameof(Icon));
            }
        }

        public UpdateAvailableResult Result { get; private set; }

        public AppCastItem CurrentItem
        {
            get
            {
                if (_updates.Count == 0) return null; // don't know why user would have opened this window with no updates, but oh well

                return _updates[0];                
            }
        }

        public bool Critical { get; }
        public bool NotCritical => !Critical;

        private Visibility _skipVisibility;

        public Visibility SkipVisibility
        {
            get => _skipVisibility;
            set
            {
                if (_skipVisibility == value) return;
                _skipVisibility = value;
                OnPropertyChanged(nameof(SkipVisibility));
            }
        }

        private Visibility _remindVisibility;

        public Visibility RemindVisibility
        {
            get => _remindVisibility;
            set
            {
                if (_remindVisibility == value) return;
                _remindVisibility = value;
                OnPropertyChanged(nameof(RemindVisibility));
            }
        }

        private Visibility _releaseNotesVisibility;

        public Visibility ReleaseNotesVisibility
        {
            get => _releaseNotesVisibility;
            set
            {
                if (_releaseNotesVisibility == value) return;
                _releaseNotesVisibility = value;
                OnPropertyChanged(nameof(ReleaseNotesVisibility));
            }
        }

        /// <summary>
        /// Template for HTML code drawing release notes separator. {0} used for version number, {1} for publication date
        /// </summary>
        private string _separatorTemplate;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Sparkle _sparkle;
        private readonly List<AppCastItem> _updates;

        private static readonly HashSet<string> MarkDownExtension = new HashSet<string> { ".md", ".mkdn", ".mkd", ".markdown" };

        public UpdateAvailableViewModel(Sparkle sparkle, List<AppCastItem> items, Uri applicationIcon = null, bool isUpdateAlreadyDownloaded = false,
            string separatorTemplate = "", string headAddition = "")
        {
            _sparkle = sparkle;
            _updates = items;

            _separatorTemplate =
                !string.IsNullOrEmpty(separatorTemplate) ?
                separatorTemplate :
                "<div style=\"border: #ccc 1px solid;\"><div style=\"background: {3}; padding: 5px;\"><span style=\"float: right; display:float;\">" +
                "{1}</span>{0}</div><div style=\"padding: 5px;\">{2}</div></div><br>";

            AppCastItem item = items.FirstOrDefault();

            var appName = item?.AppName ?? "the application";

            var version = "";
            try
            {
                // Use try/catch since Version constructor can throw an exception and we don't want to
                // die just because the user has a malformed version string
                Version versionObj = new Version(item.AppVersionInstalled);
                version = Utilities.GetVersionString(versionObj);
            }
            catch
            {
            }

            var action = isUpdateAlreadyDownloaded ? "install" : "download";

            _header = $"A new version of {appName} is available.";

            _info = $"{appName} {item?.Version ?? string.Empty} is now available (you have {version}). Would you like to {action} it now?";

            _iconUri = applicationIcon;

            AppCastItem latestVersion = items.OrderByDescending(p => p.Version).FirstOrDefault();

            string initialHTML = "<html><head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'>" + headAddition + "</head><body>";
            ReleaseNotes = initialHTML + "<p><em>Loading release notes...</em></p></body></html>";

            Critical = items.Any(x => x.IsCriticalUpdate);

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            DownloadAndDisplayAllReleaseNotes(items, latestVersion, initialHTML);
        }

        private async void DownloadAndDisplayAllReleaseNotes(List<AppCastItem> items, AppCastItem latestVersion, string initialHTML)
        {

            _sparkle.LogWriter.PrintMessage("Preparing to initialize release notes...");
            StringBuilder sb = new StringBuilder(initialHTML);
            foreach (AppCastItem castItem in items)
            {
                _sparkle.LogWriter.PrintMessage("Initializing release notes for {0}", castItem.Version);
                // TODO: could we optimize this by doing multiple downloads at once?
                var releaseNotes = await GetReleaseNotes(castItem);
                sb.Append(string.Format(_separatorTemplate,
                                        castItem.Version,
                                        castItem.PublicationDate.ToString("D"), // was dd MMM yyyy
                                        releaseNotes,
                                        latestVersion.Version.Equals(castItem.Version) ? "#ABFF82" : "#AFD7FF"));
            }
            sb.Append("</body>");

            string fullHTML = sb.ToString();
            ReleaseNotes = fullHTML;
            //ReleaseNotesBrowser.Invoke((MethodInvoker)delegate
            //{
            //    // see https://stackoverflow.com/a/15209861/3938401
            //    ReleaseNotesBrowser.Navigate("about:blank");
            //    ReleaseNotesBrowser.Document.OpenNew(true);
            //    ReleaseNotesBrowser.Document.Write(fullHTML);
            //    ReleaseNotesBrowser.DocumentText = fullHTML;
            //});
            _sparkle.LogWriter.PrintMessage("Done initializing release notes!");
        }

        private async Task<string> GetReleaseNotes(AppCastItem item)
        {
            string criticalUpdate = item.IsCriticalUpdate ? "Critical Update" : "";
            // at first try to use embedded description
            if (!string.IsNullOrEmpty(item.Description))
            {
                // check for markdown
                Regex containsHtmlRegex = new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>");
                if (containsHtmlRegex.IsMatch(item.Description))
                {
                    if (item.IsCriticalUpdate)
                    {
                        item.Description = "<p><em>" + criticalUpdate + "</em></p>" + "<br>" + item.Description;
                    }
                    return item.Description;
                }
                else
                {
                    var md = new MarkdownSharp.Markdown();
                    if (item.IsCriticalUpdate)
                    {
                        item.Description = "*" + criticalUpdate + "*" + "\n\n" + item.Description;
                    }
                    var temp = md.Transform(item.Description);
                    return temp;
                }
            }

            // not embedded so try to release notes from the link
            if (string.IsNullOrEmpty(item.ReleaseNotesLink))
            {
                return null;
            }

            // download release notes
            _sparkle.LogWriter.PrintMessage("Downloading release notes for {0} at {1}", item.Version, item.ReleaseNotesLink);
            string notes = await DownloadReleaseNotes(item.ReleaseNotesLink, _cancellationToken);
            _sparkle.LogWriter.PrintMessage("Done downloading release notes for {0}", item.Version);
            if (string.IsNullOrEmpty(notes))
            {
                return null;
            }

            // check dsa of release notes
            if (!string.IsNullOrEmpty(item.ReleaseNotesDSASignature))
            {
                if (_sparkle.DSAChecker.VerifyDSASignatureOfString(item.ReleaseNotesDSASignature, notes) == ValidationResult.Invalid)
                    return null;
            }

            // process release notes
            var extension = Path.GetExtension(item.ReleaseNotesLink);
            if (extension != null && MarkDownExtension.Contains(extension.ToLower()))
            {
                try
                {
                    var md = new MarkdownSharp.Markdown();
                    if (item.IsCriticalUpdate)
                    {
                        notes = "*" + criticalUpdate + "*" + "\n\n" + notes;
                    }
                    notes = md.Transform(notes);
                }
                catch (Exception ex)
                {
                    _sparkle.LogWriter.PrintMessage("Error parsing Markdown syntax: {0}", ex.Message);
                }
            }
            return notes;
        }

        private async Task<string> DownloadReleaseNotes(string link, CancellationToken cancellationToken)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                    webClient.Encoding = Encoding.UTF8;
                    if (cancellationToken != null)
                    {
                        using (cancellationToken.Register(() => webClient.CancelAsync()))
                        {
                            return await webClient.DownloadStringTaskAsync(Utilities.GetAbsoluteURL(link, _sparkle.AppcastUrl));
                        }
                    }
                    return await webClient.DownloadStringTaskAsync(Utilities.GetAbsoluteURL(link, _sparkle.AppcastUrl));
                }
            }
            catch (WebException ex)
            {
                _sparkle.LogWriter.PrintMessage("Cannot download release notes from {0} because {1}", link, ex.Message);
                return "";
            }
        }

        public void HideReleaseNotes()
        {
            ReleaseNotesVisibility = Visibility.Collapsed;
        }

        public void HideRemindMeLaterButton()
        {
            RemindVisibility = Visibility.Hidden;
        }

        public void HideSkipButton()
        {
            SkipVisibility = Visibility.Hidden;
        }

        public void Close()
        {
            _cancellationTokenSource?.Cancel();
        }

        #region SkipCommand
        private RelayCommand _skipCommand;
        public ICommand SkipCommand => _skipCommand ?? (_skipCommand = new RelayCommand(OnSkip, CanSkip));

        private bool CanSkip(object parameter)
        {
            return true;
        }

        private void OnSkip(object parameter)
        {
            OnUserResponded(UpdateAvailableResult.SkipUpdate);
        }

        #endregion SkipCommand

        #region RemindCommand
        private RelayCommand _remindCommand;
        public ICommand RemindCommand => _remindCommand ?? (_remindCommand = new RelayCommand(OnRemind, CanRemind));

        private bool CanRemind(object parameter)
        {
            return true;
        }

        private void OnRemind(object parameter)
        {
            OnUserResponded(UpdateAvailableResult.RemindMeLater);
        }

        #endregion RemindCommand

        #region InstallCommand
        private RelayCommand _installCommand;
        public ICommand InstallCommand => _installCommand ?? (_installCommand = new RelayCommand(OnInstall, CanInstall));

        private bool CanInstall(object parameter)
        {
            return true;
        }

        private void OnInstall(object parameter)
        {
            OnUserResponded(UpdateAvailableResult.InstallUpdate);
        }

        #endregion InstallCommand

        #region UserResponded

        public event EventHandler UserResponded;

        protected void OnUserResponded(UpdateAvailableResult result)
        {
            Result = result;
            UserResponded?.Invoke(this, new EventArgs());
        }

        #endregion

    }
}
