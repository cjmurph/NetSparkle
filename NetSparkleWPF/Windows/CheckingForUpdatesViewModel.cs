using NetSparkleWPF.ViewModel;
using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetSparkleWPF.Windows
{
    public class CheckingForUpdatesViewModel:ObservableObject
    {
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

        public CheckingForUpdatesViewModel(Uri applicationIcon)
        {
            _iconUri = applicationIcon ?? SparkleWPF.DefaultIcon;
        }

        #region CancelCommand
        private RelayCommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ?? (_cancelCommand = new RelayCommand(OnCancel, CanCancel));

        private bool CanCancel(object parameter)
        {
            return true;
        }

        private void OnCancel(object parameter)
        {
            
        }

        #endregion CancelCommand
    }
}
