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

## Notes

When the **AppDomain.UnhandledException** handler is not set, only the exception that occurs on the main UI thread of the application are handled by the **Application.DispatcherUnhandledException** (see [http://msdn.microsoft.com/en-us/library/system.windows.application.dispatcherunhandledexception(v=vs.100).aspx](http://msdn.microsoft.com/en-us/library/system.windows.application.dispatcherunhandledexception(v=vs.100).aspx)). Therefor it is highly recommended to provided a AppDomain to the crash report.  
This library doesn't (yet) provide a way to Handle exception that occurs on the UI thread, with the property Handled of the event.

## What's next

There is a lot of things missing, so here is a list of thing to do:  

* No default icon, it take the Application MainWindow icon, or no icon, if none is provided
* Ability to prompt the user right after the crash report instead of the next start
* Related to the previous point, ability to handle crash, so the application doesn't get killed

