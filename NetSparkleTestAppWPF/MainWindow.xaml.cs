using NetSparkleWPF;
using System;
using System.Drawing;
using System.Windows;


namespace NetSparkle.TestAppWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SparkleWPF _sparkle;

        public MainWindow()
        {
            InitializeComponent();

            // remove the netsparkle key from registry 
            try
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree("Software\\Microsoft\\NetSparkle.TestAppWPF");
            }
            catch { }

            var icon = new Uri("pack://application:,,,/software-update-available.ico");
            //// set icon in project properties!
            //string manifestModuleName = System.Reflection.Assembly.GetEntryAssembly().ManifestModule.FullyQualifiedName;
            //var icon = System.Drawing.Icon.ExtractAssociatedIcon(manifestModuleName);
            _sparkle = new SparkleWPF("https://deadpikle.github.io/NetSparkle/files/sample-app/appcast.xml", icon);
            // TLS 1.2 required by GitHub (https://developer.github.com/changes/2018-02-01-weak-crypto-removal-notice/)
            _sparkle.SecurityProtocolType = System.Net.SecurityProtocolType.Tls12;
            _sparkle.ParentWindow = this;
            _sparkle.StartLoop(true, true);
        }

        private void ManualUpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            _sparkle.CheckForUpdatesAtUserRequest();
        }
    }
}
