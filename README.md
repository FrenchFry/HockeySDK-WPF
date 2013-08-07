This is an implementation of the [HockeyApp SDK](https://github.com/bitstadium/HockeySDK-iOS) for .NET.

## Requirements

* .NET Framework 4.0 Client Profile
* A WPF Application

## Import Library

* Download the latest version of HockeySDK-.NET
* Build the project
* Copy the HockeySDK.dll to your project directory
* Open your solution in Visual Studio
* Right-click the group "References" and select "Add Reference".
* Choose the tab "Browse", search for the HockeySDK.dll and confirm with OK.

## Modify Code

* Open your App.xaml.cs
* In the App constructor add the following line: <pre>HockeyApp.CrashHandler.Instance.Configure(this, APP_ID, AppName, CompanyName);</pre>
* Replace APP_ID with the App ID of your application. You can find it on the HockeyApp website.
* Replace AppName and CompanyName with your informations.
* Open the window from which you want to check for crashes, for example MainWindow.xaml.cs.
* In the constructor, add the following line: <pre>HockeyApp.CrashHandler.Instance.HandleCrashes(false);</pre>
* Build and Run.

Now every time an unhandled exception occurs, the app will be killed, which is the normal behavior. At the next start the user will be prompted to send a crash report.

## Configure

* Change the **Package** property to match the Bundle Identifier specified on HockeyApp. If the Package doesn't match crash report will not appear on HockeyApp. (By default the library will take the namespace of the application specified in the **Configure** method).

* If you want to get crash report for AppDomain UnhandledException, you should use the Configure methods this way:
	<pre>HockeyApp.CrashHandler.Instance.Configure(this, anAppDomain, APP_ID, AppName, CompanyName);</pre>
Most of the time you should just replace **anAppDomain** by **AppDomain.CurrentDomain**.

* Change the IndicateAppDomainException value to **true** if you want your crash report to indicate that the exception was catch by this event.

* You can specify custom handlers for the **Application.DispatcherUnhandledException** and the **AppDomain.UnhandledException** handlers. This will allow you to perform custom operations (like for example preventing crash of the application, by handling exception). See Notes for more informations.

## Notes

When the **AppDomain.UnhandledException** handler is not set, only the exception that occurs on the main UI thread of the application are handled by the **Application.DispatcherUnhandledException** (see [msdn](http://msdn.microsoft.com/en-us/library/system.windows.application.dispatcherunhandledexception(v=vs.100).aspx)). Therefor it is highly recommended to provided an AppDomain to the crash report.

If you set an AppDomain all exceptions will go through the **AppDomain.UnhandledException** handler. So for example if you set a custom handler for **Application.DispatcherUnhandledException** and doesn't set the Handled property to **true**, the application will crash. Therefor, if you use one custom handler, it is highly encouraged to use also the other one, since they work best together.

## What's next  

* No default icon, it take the Application MainWindow icon, or no icon, if none is provided
* Ability to prompt the user right after the crash report instead of the next start
* Give access to methods for generating crash report and send them. So this can be used in custom handlers.

