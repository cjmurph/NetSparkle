using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NetSparkleWPF.Windows
{
    /// <summary>
    /// Interaction logic for MessageNotificationWindow.xaml
    /// </summary>
    public partial class MessageNotificationWindow : Window
    {
        public string MessageTitle
        {
            get { return (string)GetValue(MessageTitleProperty); }
            set { SetValue(MessageTitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MessageTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageTitleProperty =
            DependencyProperty.Register("MessageTitle", typeof(string), typeof(MessageNotificationWindow), new PropertyMetadata("Message"));


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(MessageNotificationWindow), new PropertyMetadata("Something happened!"));


        private Uri _iconUri;

        public Uri IconUri
        {
            get => _iconUri;
            set
            {
                if (_iconUri == value) return;
                _iconUri = value;
                Icon = BitmapFrame.Create(IconUri);
            }
        }

        public MessageNotificationWindow(string title, string message, Uri applicationIcon = null)
        {
            IconUri = applicationIcon ?? SparkleWPF.DefaultIcon;
            MessageTitle = title;
            Message = message;
            InitializeComponent();
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
