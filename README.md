This is an implementation of the [HockeyApp SDK](https://github.com/bitstadium/HockeySDK-iOS) for .NET.

## Requirements

* .NET Framework 4.0 Client Profile
* A WPF Application

## Import Library

* Download the latest version of HockeySDK-.NET
* Unzip the file.
* Copy the HockeySDK.dll to your project directory
* Open your solution in Visual Studio
* Right-click the group "References" and select "Add Reference".
* Choose the tab "Browse", search for the HockeySDK.dll and confirm with OK.

## Modify Code

* Open your App.xaml.cs
* In the App constructor add the following line: <pre>HockeyApp.CrashHandler.Instance.Configure(this, APP_ID);</pre>
* Replace APP_ID with the App ID of your application. You can find it on the HockeyApp website.
* Open the window from which you want to check for crashes, for example MainWindow.xaml.cs.
* In the constructor, add the following line: <pre>HockeyApp.CrashHandler.Instance.HandleCrashes(false);</pre>
* Build and Run.

Now every time an unhandled exception occurs, the app is killed. At the next start the user will be prompted to send a crash report.

## Configure

* Change the **Package** property to match the Bundle Identifier specified on HockeyApp. If the Package doesn't match crash report will not appear on HockeyApp. (By default the library will take the namespace of the application specified in the **Configure** method).

## What's next

There is a lot of things missing, so here is a list of thing to do:  

* Name, Email and Comment fields are not used
* The name of the application and the developer is not used in the crash report window
* No default icon, it take the Application MainWindow icon
* Ability to prompt the user right after the crash report instead of the next start
* Related to the previous point, ability to handle crash, so the application doesn't get killed
* Only the **Application.DispatcherUnhandledException** are handled now, **AppDomain.UnhandledException** should also be handled
* Support for more language

