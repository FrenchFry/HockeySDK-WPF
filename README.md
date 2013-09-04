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
* In the App constructor add the following line: <pre>HockeyApp.CrashHandler.Instance.Configure(this, anAppDomain, APP_ID, appName, companyName);</pre>
* Replace **APP_ID** with the App ID of your application. You can find it on the HockeyApp website.
* Replace **appName** and **companyName** with your informations.
* Replace **anAppDomain** by **AppDomain.CurrentDomain** (this should work well most of the time).
* Open the window from which you want to check for crashes, for example MainWindow.xaml.cs.
* In the constructor, add the following line: <pre>HockeyApp.CrashHandler.Instance.HandleCrashes(false);</pre>
* Build and Run.

Now every time an unhandled exception occurs, the app will be killed, which is the normal behavior. At the next start the user will be prompted to send a crash report.

## Configure

* Change the **Package** property to match the Bundle Identifier specified on HockeyApp. If the Package doesn't match crash report will not appear on HockeyApp. (By default the library will take the namespace of the application specified in the **Configure** method).

## Notes

To match as much as possible the common behavior of HockeyApp, this library only handled **AppDomain.UnhandledException**, which means the application will crash after receiving an exception and on restart, the user will be prompted to send a crash report.

However, if you'd like to handle **Application.DispatcherUnhandledException**, to avoid crash of the application, but still want to log the exception. You can add a normal event handler and use the **LogException** method provided by the crash reporter.

## What's next  

* No default icon, it take the Application MainWindow icon, or no icon, if none is provided
