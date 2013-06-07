using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Media;

namespace HockeyApp.Controls
{
    /// <summary>
    /// Interaction logic for CrashReporter
    /// </summary>
    public partial class CrashReporter : Window
    {
        #region Constructors

        /// <summary>
        /// Initializes new instance of the CrashReporter class.
        /// </summary>
        public CrashReporter()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the CrashReporter class.
        /// </summary>
        /// <param name="filepath">The path to the crash log file.</param>
        /// <param name="icon">The application icon.</param>
        /// <param name="applicationName">The application name.</param>
        /// <param name="developerName">The developer name.</param>
        public CrashReporter(string filepath, ImageSource icon, string applicationName, string developerName)
            : this()
        {
            if (filepath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            if (icon == null)
            {
                throw new ArgumentNullException("icon");
            }

            if (applicationName == null)
            {
                throw new ArgumentNullException("applicationName");
            }

            if (developerName == null)
            {
                throw new ArgumentNullException("developerName");
            }

            this.Icon = icon;

            this.AppIcon.Source = icon;

            this.Title = string.Format(Properties.Resources.windowTitle, applicationName);
            this.Header.Text = string.Format(Properties.Resources.instructionText, applicationName, developerName);
            this.NameLabel.Text = Properties.Resources.nameLabel;
            this.EmailLabel.Text = Properties.Resources.emailLabel;
            this.CommentsLabel.Text = Properties.Resources.commentsLabel;
            this.Details.Header = Properties.Resources.detailsLabel;
            this.Cancel.Content = Properties.Resources.cancel;
            this.Send.Content = Properties.Resources.send;
            this.Privacy.Text = Properties.Resources.privacyNote;

            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly();
            Stream fileStream = store.OpenFile(filepath, FileMode.Open);
            string log = string.Empty;
            using (StreamReader reader = new StreamReader(fileStream))
            {
                log = reader.ReadToEnd();
            }

            this.DetailsContent.Text = log;
        }

        #endregion

        #region Properties

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public string UserComments { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Handler for the button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arg.</param>
        private void SendCrashReport(object sender, RoutedEventArgs e)
        {
            this.UserName = this.NameTextBox.Text;
            this.UserEmail = this.EmailTextBox.Text;
            this.UserComments = this.CommentsTextBox.Text;

            this.DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
