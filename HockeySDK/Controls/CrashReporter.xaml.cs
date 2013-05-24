using System.IO;
using System.Windows;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using System.Windows.Media;

namespace HockeyApp.Controls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class CrashReporter : Window
    {
        public CrashReporter()
        {
            InitializeComponent();
        }

        public CrashReporter(string filepath, ImageSource icon)
            : this()
        {
            this.Icon = icon;

            this.AppIcon.Source = icon;

            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly();
            Stream fileStream = store.OpenFile(filepath, FileMode.Open);
            string log = "";
            using (StreamReader reader = new StreamReader(fileStream))
            {
                log = reader.ReadToEnd();
            }

            this.Details.Text = log;
        }

        private void SendCrashReport(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
