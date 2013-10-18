using System;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using HockeyApp.Controls;

namespace HockeyApp
{
    /// <summary>
    /// The CrashHandler manages the creation and deletion of crash logs, when the application
    /// receive an exception that has not been handled.
    /// It is also responsible for the presentation of the crash reporter to the user.
    /// </summary>
    public sealed class CrashHandler
    {
        #region Fields

        /// <summary>
        /// The name of the crash logs directory.
        /// </summary>
        private const string CrashDirectoryName = "CrashLogs";

        /// <summary>
        /// The name of the SDK.
        /// </summary>
        private const string SdkName = "HockeySDK";

        /// <summary>
        /// The version of the SDK.
        /// </summary>
        private const string SdkVersion = "1.0";

        private static object padlock = new object();
        private static CrashHandler instance;

        /// <summary>
        /// The application attached to the crash reporter.
        /// </summary>
        private Application application;

        /// <summary>
        /// The app domain attached to the crash reporter.
        /// </summary>
        private AppDomain appDomain;

        /// <summary>
        /// The HockeyApp app identifier.
        /// </summary>
        private string identifier;

        /// <summary>
        /// The app name.
        /// </summary>
        private string applicationName;

        /// <summary>
        /// The developer or company name.
        /// </summary>
        private string developerName;

        /// <summary>
        /// The user name filled in the crash reporter window.
        /// </summary>
        private string userName;

        /// <summary>
        /// The user email filled in the crash reporter window.
        /// </summary>
        private string userEmail;

        /// <summary>
        /// The user comments filled in the crash reporter window.
        /// </summary>
        private string userComments;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the unique instance of the CrashReporter.
        /// </summary>
        public static CrashHandler Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new CrashHandler();
                    }

                    return instance;
                }
            }
        }

        /// <summary>
        /// Gets or sets a string that indicates the name of the Package used in
        /// the crash report.
        /// If not specified, the Package will be the Application main name space.
        /// You can use this property to specify a different Package name.
        /// </summary>
        public string Package { get; set; }

        #endregion

        /// <summary>
        /// Configure the crash reporter, each parameter are required.
        /// </summary>
        /// <param name="application">The application associated to the crash reporter.</param>
        /// <param name="appDomain">The app domain from which the exception will be handled.</param>
        /// <param name="identifier">The HockeyApp application identifier.</param>
        /// <param name="applicationName">The name of the application.</param>
        /// <param name="developerName">The developer or company name.</param>
        public void Configure(Application application, AppDomain appDomain, string identifier, string applicationName, string developerName)
        {
            if (application == null)
            {
                throw new ArgumentNullException("application");
            }

            if (appDomain == null)
            {
                throw new ArgumentNullException("appDomain");
            }

            if (identifier == null)
            {
                throw new ArgumentNullException("identifier");
            }

            if (applicationName == null)
            {
                throw new ArgumentNullException("applicationName");
            }

            if (developerName == null)
            {
                throw new ArgumentNullException("developerName");
            }

            if (this.application == null)
            {
                this.application = application;
                this.identifier = identifier;
                this.applicationName = applicationName;
                this.developerName = developerName;
                this.appDomain = appDomain;

                this.appDomain.UnhandledException += this.OnUnhandledException;
            }
            else
            {
                throw new InvalidOperationException("CrashHandler was already configured!");
            }
        }

        /// <summary>
        /// Handles the crash report.
        /// When the application crashes, a crash log will be store on the user hard drive,
        /// on launch, calling HandleCrashes will prompt the user, and ask him if it want
        /// to send a crash report to the developer. This can be bypassed by passing true as parameter
        /// to this method.
        /// </summary>
        /// <param name="sendAutomatically">Indicates whether the log should be send without asking for user consent.</param>
        public void HandleCrashes(bool sendAutomatically)
        {
            try
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly();
                if (store.DirectoryExists(CrashDirectoryName))
                {
                    string[] filenames = store.GetFileNames(CrashDirectoryName + "\\crash*.log");
                    if (filenames.Length > 0)
                    {
                        if (sendAutomatically)
                        {
                            this.SendCrashes(store, filenames);
                        }
                        else
                        {
                            CrashReporter reporter = new CrashReporter(
                                Path.Combine(CrashDirectoryName, filenames[filenames.Length - 1]),
                                this.application.MainWindow.Icon,
                                this.applicationName,
                                this.developerName);
                            reporter.ShowDialog();

                            if (reporter.DialogResult == true)
                            {
                                this.userName = reporter.UserName;
                                this.userEmail = reporter.UserEmail;
                                this.userComments = reporter.UserComments;
                                this.SendCrashes(store, filenames);
                            }
                            else
                            {
                                this.DeleteCrashes(store, filenames);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore all exceptions
            }
        }

        /// <summary>
        /// Allows to log an exception not directly handled by this Crash Reporter.
        /// </summary>
        /// <param name="e">The exception to log.</param>
        /// <param name="customInformation">Custom information to display in the crash report,it will be displayed after the crash report date.</param>
        public void LogException(Exception e, string customInformation)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.CreateHeader(customInformation));
            builder.AppendLine();
            builder.Append(this.CreateStackTrace(e));
            this.SaveLog(builder.ToString());
        }

        /// <summary>
        /// Handler for the UnhandledException event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args.</param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.CreateHeader(null));
            builder.AppendLine();
            builder.Append(this.CreateStackTrace(e.ExceptionObject as Exception));
            this.SaveLog(builder.ToString());
        }

        /// <summary>
        /// Creates the header portion of the crash log.
        /// </summary>
        /// <param name="customInformation">A string that will be displayed in the crash log. Can be used to provide more information.</param>
        /// <returns>A string that represents the header part of the crash log.</returns>
        private string CreateHeader(string customInformation)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(this.Package))
            {
                stringBuilder.AppendFormat("Package: {0}\n", this.Package);
            }
            else
            {
                stringBuilder.AppendFormat("Package: {0}\n", this.application.GetType().Namespace);
            }

            stringBuilder.AppendFormat("Version: {0}\n", this.application.GetType().Assembly.GetName().Version.ToString());
            stringBuilder.AppendFormat("OS: {0}.{1}\n", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor);
            stringBuilder.AppendFormat("CLR Version: {0}\n", Environment.Version.ToString());
            stringBuilder.AppendFormat("OS Language: {0}\n", CultureInfo.InstalledUICulture.Name);

            string bitness = "32-Bit";
            if (Environment.Is64BitOperatingSystem == true)
            {
                bitness = "64-Bit";
            }

            stringBuilder.AppendFormat("OS Bitness: {0}\n", bitness);
            stringBuilder.AppendFormat("Date: {0}\n", DateTime.UtcNow.ToString("o"));

            if (!string.IsNullOrEmpty(customInformation))
            {
                stringBuilder.AppendFormat(customInformation);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Creates the stack trace from the handled exception.
        /// </summary>
        /// <param name="exception">The exception from which the stack trace is created.</param>
        /// <returns>A string representing the stack trace.</returns>
        private string CreateStackTrace(Exception exception)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(exception.GetType().ToString());
            builder.Append(": ");
            builder.Append(string.IsNullOrEmpty(exception.Message) ? "No reason" : exception.Message);
            builder.AppendLine();
            builder.Append(string.IsNullOrEmpty(exception.StackTrace) ? "  at unknown location" : exception.StackTrace);

            Exception inner = exception.InnerException;
            if ((inner != null) && (!string.IsNullOrEmpty(inner.StackTrace)))
            {
                builder.AppendLine();
                builder.AppendLine("Inner Exception");
                builder.Append(inner.StackTrace);
            }

            return builder.ToString().Trim();
        }

        /// <summary>
        /// Saves the log on the user hard drive. Logs are store in an Isolated Storage.
        /// </summary>
        /// <param name="log">The string representing the log file.</param>
        private void SaveLog(string log)
        {
            try
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly();
                if (!store.DirectoryExists(CrashDirectoryName))
                {
                    store.CreateDirectory(CrashDirectoryName);
                }

                string filename = string.Format("crash{0}.log", Guid.NewGuid());
                FileStream stream = store.CreateFile(Path.Combine(CrashDirectoryName, filename));
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write(log);
                }

                stream.Close();
            }
            catch
            {
                // Ignore all exceptions
            }
        }

        /// <summary>
        /// Sends the crash report to HockeyApp.
        /// </summary>
        /// <param name="store">The IsolatedStorage where the logs are store.</param>
        /// <param name="filenames">The name of each file logs.</param>
        private void SendCrashes(IsolatedStorageFile store, string[] filenames)
        {
            foreach (string filename in filenames)
            {
                try
                {
                    Stream fileStream = store.OpenFile(Path.Combine(CrashDirectoryName, filename), FileMode.Open);
                    string log = string.Empty;
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        log = reader.ReadToEnd();
                    }

                    string body = string.Empty;
                    body += "raw=" + System.Uri.EscapeDataString(log);
                    body += "&sdk=" + SdkName;
                    body += "&sdk_version=" + SdkVersion;
                    if (this.userName != null && this.userName != string.Empty)
                    {
                        body += "&userID=" + System.Uri.EscapeDataString(this.userName);
                    }

                    if (this.userEmail != null && this.userEmail != string.Empty)
                    {
                        body += "&contact=" + System.Uri.EscapeDataString(this.userEmail);
                    }

                    if (this.userComments != null && this.userComments != string.Empty)
                    {
                        body += "&description=" + System.Uri.EscapeDataString(this.userComments);
                    }

                    fileStream.Close();

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://rink.hockeyapp.net/api/2/apps/" + this.identifier + "/crashes"));
                    request.Method = WebRequestMethods.Http.Post;
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.UserAgent = "Hockey/Windows";

                    Thread tr = new Thread(() =>
                    {
                        bool deleteCrashes = true;

                        try
                        {
                            Stream requestStream = request.GetRequestStream();
                            byte[] byteArray = Encoding.UTF8.GetBytes(body);
                            requestStream.Write(byteArray, 0, body.Length);
                            requestStream.Close();

                            WebResponse resp = request.GetResponse();
                        }
                        catch (WebException e)
                        {
                            if ((e.Status == WebExceptionStatus.ConnectFailure) ||
                                (e.Status == WebExceptionStatus.ReceiveFailure) ||
                                (e.Status == WebExceptionStatus.SendFailure) ||
                                (e.Status == WebExceptionStatus.Timeout) ||
                                (e.Status == WebExceptionStatus.UnknownError))
                            {
                                deleteCrashes = false;
                            }
                        }
                        finally
                        {
                            if (deleteCrashes)
                            {
                                this.DeleteCrashes(store, filenames);
                            }
                        }
                    });

                    tr.Start();
                }
                catch (Exception)
                {
                    store.DeleteFile(Path.Combine(CrashDirectoryName, filename));
                }
            }
        }

        /// <summary>
        /// Delete all crashes logs from the user hard drive.
        /// </summary>
        /// <param name="store">The Isolated Storage where the files are stored.</param>
        /// <param name="filenames">The logs file names.</param>
        private void DeleteCrashes(IsolatedStorageFile store, string[] filenames)
        {
            foreach (string filename in filenames)
            {
                store.DeleteFile(Path.Combine(CrashDirectoryName, filename));
            }
        }
    }
}
