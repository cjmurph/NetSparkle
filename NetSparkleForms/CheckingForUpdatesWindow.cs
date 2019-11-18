using System;
using System.Drawing;
using System.Windows.Forms;
using NetSparkle.Interfaces;

namespace NetSparkleForms
{
    /// <summary>
    /// The checking for updates window
    /// </summary>
    public partial class CheckingForUpdatesWindow : Form, ICheckingForUpdates
    {
        /// <summary>
        /// Default constructor for CheckingForUpdatesWindow
        /// </summary>
        public CheckingForUpdatesWindow()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedDialog;
            FormClosing += CheckingForUpdatesWindow_FormClosing;
        }

        private void CheckingForUpdatesWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            FormClosing -= CheckingForUpdatesWindow_FormClosing;
            UpdatesUIClosing?.Invoke(sender, new EventArgs());
        }

        /// <summary>
        /// Initializes window and sets the icon to <paramref name="applicationIcon"/>
        /// </summary>
        /// <param name="applicationIcon">The icon to use</param>
        public CheckingForUpdatesWindow(Icon applicationIcon = null)
        {
            InitializeComponent();
            if (applicationIcon != null)
            {
                Icon = applicationIcon;
                iconImage.Image = new Icon(applicationIcon, new Size(48, 48)).ToBitmap();
            }
            FormBorderStyle = FormBorderStyle.FixedDialog;
        }

        public event EventHandler UpdatesUIClosing;

        private void Cancel_Click(object sender, EventArgs e)
        {
            CloseForm();
        }

        private void CloseForm()
        {
            if (InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate () { Close(); });
            }
            else
            {
                Close();
            }
        }
    }
}
