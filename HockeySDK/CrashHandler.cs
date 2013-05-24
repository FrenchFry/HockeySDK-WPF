using System;
using System.Management;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.IO;
using HockeyApp.Controls;
using System.Net;

namespace HockeyApp
{
    public sealed class CrashHandler
    {
        #region Fields

        private const String CrashDirectoryName = "CrashLogs";
        private const string SdkName = "HockeySDK";
        private const string SdkVersion = "1.0";

        private static object padlock = new object();
        private static CrashHandler instance;

        private Application application;
        private string identifier;

        #endregion

        #region Properties

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

        public string Package { get; set; }

        #endregion

        public void Configure(Application application, string identifier)
        {
            if (this.application == null)
            {
                this.application = application;
                this.identifier = identifier;

                this.application.DispatcherUnhandledException += this.OnDispatcherUnhandledException;
            }
            else
            {
                throw new InvalidOperationException("CrashHandler was already configured!");
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(CreateHeader());
            builder.AppendLine();
            builder.Append(CreateStackTrace(e));
            SaveLog(builder.ToString());
        }

        private string CreateHeader()
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
            stringBuilder.AppendFormat("Windows: {0}.{1}.{2}\n", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, Environment.OSVersion.Version.Build);
            stringBuilder.AppendFormat("CLR Version: {0}\n", Environment.Version.ToString());
            stringBuilder.AppendFormat("OS Language: {0}\n", CultureInfo.InstalledUICulture.Name);
            
            string bitness = "32-Bit";
            if (Environment.Is64BitOperatingSystem == true)
            {
                bitness = "64-Bit";
            }

            stringBuilder.AppendFormat("OS Bitness: {0}\n", bitness);
            stringBuilder.AppendFormat("Date: {0}\n", DateTime.UtcNow.ToString());

            return stringBuilder.ToString();
        }

        private String CreateStackTrace(DispatcherUnhandledExceptionEventArgs e)
        {
            Exception exception = e.Exception;
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

        private void SaveLog(String log)
        {
            try
            {
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly();
                if (!store.DirectoryExists(CrashDirectoryName))
                {
                    store.CreateDirectory(CrashDirectoryName);
                }

                String filename = string.Format("crash{0}.log", Guid.NewGuid());
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

        public void HandleCrashes(Boolean sendAutomatically)
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
                            CrashReporter reporter = new CrashReporter(Path.Combine(CrashDirectoryName, filenames[filenames.Length - 1]), this.application.MainWindow.Icon);
                            reporter.ShowDialog();

                            if (reporter.DialogResult == true)
                            {
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

        private void SendCrashes(IsolatedStorageFile store, string[] filenames)
        {
            foreach (String filename in filenames)
            {
                try
                {
                    Stream fileStream = store.OpenFile(Path.Combine(CrashDirectoryName, filename), FileMode.Open);
                    string log = "";
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        log = reader.ReadToEnd();
                    }

                    string body = "";
                    body += "raw=" + System.Uri.EscapeDataString(log);
                    body += "&sdk=" + SdkName;
                    body += "&sdk_version=" + SdkVersion;

                    fileStream.Close();

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://rink.hockeyapp.net/api/2/apps/" + identifier + "/crashes"));
                    request.Method = WebRequestMethods.Http.Post;   
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.UserAgent = "Hockey/Windows";

                    request.BeginGetRequestStream(requestResult =>
                    {
                        try
                        {
                            Stream stream = request.EndGetRequestStream(requestResult);
                            byte[] byteArray = Encoding.UTF8.GetBytes(body);
                            stream.Write(byteArray, 0, body.Length);
                            stream.Close();

                            request.BeginGetResponse(responseResult =>
                            {
                                Boolean deleteCrashes = true;
                                try
                                {
                                    request.EndGetResponse(responseResult);
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
                                catch (Exception)
                                {
                                }
                                finally
                                {
                                    if (deleteCrashes)
                                    {
                                        this.DeleteCrashes(store, filenames);
                                    }
                                }
                            }, null);
                        }
                        catch (Exception)
                        {
                        }
                    }, null);
                }
                catch (Exception)
                {
                    store.DeleteFile(Path.Combine(CrashDirectoryName, filename));
                }
            }
        }

        private void DeleteCrashes(IsolatedStorageFile store, string[] filenames)
        {
            foreach (String filename in filenames)
            {
                store.DeleteFile(Path.Combine(CrashDirectoryName, filename));
            }
        }
    }
}
