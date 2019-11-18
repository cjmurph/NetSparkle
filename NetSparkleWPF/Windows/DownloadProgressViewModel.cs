using NetSparkle;
using NetSparkle.Events;
using NetSparkleWPF.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetSparkleWPF.Windows
{
    public class DownloadProgressViewModel:ObservableObject
    {
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

        public ImageSource Icon
        {
            get { return BitmapFrame.Create(IconUri); }
        }

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

        private int _progress;

        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress == value) return;
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private string _progressMessage;

        public string ProgressMessage
        {
            get => _progressMessage;
            set
            {
                if (_progressMessage == value) return;
                _progressMessage = value;
                OnPropertyChanged(nameof(ProgressMessage));
            }
        }

        private string _cancelText = "Cancel";

        public string CancelText
        {
            get => _cancelText;
            set
            {
                if (_cancelText == value) return;
                _cancelText = value;
                OnPropertyChanged(nameof(CancelText));
            }
        }

        public Visibility ProgressBarVisibility => Downloading ? Visibility.Visible : Visibility.Hidden;

        private Visibility _installButtonVisibility;

        public Visibility InstallButtonVisibility
        {
            get => _installButtonVisibility;
            set
            {
                if (_installButtonVisibility == value) return;
                _installButtonVisibility = value;
                OnPropertyChanged(nameof(InstallButtonVisibility));
            }
        }

        private bool _installButtonEnabled = true;

        public bool InstallButtonEnabled
        {
            get => !Downloading && _installButtonEnabled;
            set
            {
                if (_installButtonEnabled == value) return;
                _installButtonEnabled = value;
                OnPropertyChanged(nameof(InstallButtonEnabled));
            }
        }

        private bool _downloading;

        public bool Downloading
        {
            get => _downloading;
            set
            {
                if (_downloading == value) return;
                _downloading = value;
                OnPropertyChanged(nameof(Downloading));
                OnPropertyChanged(nameof(InstallButtonEnabled));
                OnPropertyChanged(nameof(ProgressBarVisibility));
            }
        }
        private AppCastItem _item;

        public DownloadProgressViewModel()
        {
            _iconUri = SparkleWPF.DefaultIcon;
            Header = "Downloading APP_NAME APP_VERSION";
            InstallButtonVisibility = Visibility.Hidden;
        }

        public DownloadProgressViewModel(AppCastItem item, Uri applicationIcon)
        {
            _iconUri = applicationIcon ?? SparkleWPF.DefaultIcon;
            Header = $"Downloading {item?.AppName ?? "NAME"} {item?.Version ?? "VERSION"}";
            _item = item;
            InstallButtonVisibility = Visibility.Hidden;
        }

        public void UpdateProgress(long bytesReceived, long totalBytesToReceive, int percentage)
        {
            Progress = percentage;
            ProgressMessage = $" ({Utilities.NumBytesToUserReadableString(bytesReceived)} / {Utilities.NumBytesToUserReadableString(totalBytesToReceive)})";
            if (percentage > 0 && percentage < 100)
                Downloading = true;
            else
                Downloading = false;
        }

        public void FinishedDownloadingFile(bool isDownloadedFileValid)
        {
            Header = $"Downloaded {_item?.AppName ?? "NAME"} {_item?.Version ?? "VERSION"}";
            ProgressMessage = "Ready to install";
            Downloading = false;
            if (isDownloadedFileValid)
            {
                InstallButtonVisibility = Visibility.Visible;
            }
            else
            {
                InstallButtonVisibility = Visibility.Hidden;
            }
        }

        public bool ShowErrorMessage(string errorMessage)
        {
            Downloading = false;
            InstallButtonVisibility = Visibility.Hidden;
            CancelText = "Close";
            ProgressMessage = errorMessage;
            return true;
        }

        #region InstallAndRelaunchCommand
        private RelayCommand _installAndRelaunchCommand;
        public ICommand InstallAndRelaunchCommand => _installAndRelaunchCommand ?? (_installAndRelaunchCommand = new RelayCommand(OnInstallAndRelaunch, CanInstallAndRelaunch));

        private bool CanInstallAndRelaunch(object parameter)
        {
            return InstallButtonEnabled;
        }

        private void OnInstallAndRelaunch(object parameter)
        {
            OnDownloadProcessCompleted(true);
        }

        #endregion InstallAndRelaunchCommand       

        #region CancelCommand
        private RelayCommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ?? (_cancelCommand = new RelayCommand(OnCancel, CanCancel));

        private bool CanCancel(object parameter)
        {
            return true;
        }

        private void OnCancel(object parameter)
        {
            OnDownloadProcessCompleted(false);
        }

        #endregion CancelCommand

        #region DownloadProcessCompleted

        public event DownloadInstallEventHandler DownloadProcessCompleted;

        protected void OnDownloadProcessCompleted(bool shouldInstall)
        {
            DownloadProcessCompleted?.Invoke(this, new DownloadInstallArgs(shouldInstall));
        }

        #endregion

    }
}
