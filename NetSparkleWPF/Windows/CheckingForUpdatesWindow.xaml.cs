using System;
using System.Windows;
using NetSparkle.Interfaces;

namespace NetSparkleWPF.Windows
{
    /// <summary>
    /// Interaction logic for CheckForUpdatesWIndow.xaml
    /// </summary>
    public partial class CheckingForUpdatesWindow : Window, ICheckingForUpdates
    {
        CheckingForUpdatesViewModel Context { get; set; }
        public CheckingForUpdatesWindow(Uri applicationIcon)
        {
            Context = new CheckingForUpdatesViewModel(applicationIcon);
            InitializeComponent();
            DataContext = Context;
            Closing += CheckingForUpdatesWindow_Closing;
        }

        private void CheckingForUpdatesWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= CheckingForUpdatesWindow_Closing;
            UpdatesUIClosing?.Invoke(this, new EventArgs());
        }

        public event EventHandler UpdatesUIClosing;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
