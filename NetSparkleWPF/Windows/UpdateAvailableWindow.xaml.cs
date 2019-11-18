using NetSparkle;
using NetSparkle.Enums;
using NetSparkle.Interfaces;
using NetSparkleWPF.Interfaces;
using NetSparkleWPF.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows;

namespace NetSparkleWPF.Windows
{
    /// <summary>
    /// Interaction logic for UpdateAvailableWindow.xaml
    /// </summary>
    public partial class UpdateAvailableWindow : Window, IUpdateAvailable
    {
        public UpdateAvailableViewModel Context { get; private set; }

        public UpdateAvailableResult Result => Context.Result;

        public AppCastItem CurrentItem => Context.CurrentItem;

        public UpdateAvailableWindow(Sparkle sparkle, List<AppCastItem> items, Uri applicationIcon = null, bool isUpdateAlreadyDownloaded = false,
            string separatorTemplate = "", string headAddition = "")
        {
            Context = new UpdateAvailableViewModel(sparkle, items, applicationIcon, isUpdateAlreadyDownloaded, separatorTemplate, headAddition);
            Context.UserResponded += (s, e) =>
            {
                UserResponded?.Invoke(this, e);
                Context.Close();
                Close();
            };
            InitializeComponent();
            DataContext = Context;
        }

        public event EventHandler UserResponded;

        public void HideReleaseNotes()
        {
            Context.HideReleaseNotes();
        }

        public void HideRemindMeLaterButton()
        {
            Context.HideRemindMeLaterButton();
        }

        public void HideSkipButton()
        {
            Context.HideSkipButton();
        }

        public void BringToFront()
        {
            Activate();
        }

        void IUpdateAvailable.Close()
        {
            Context.Close();
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
