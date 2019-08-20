# Foreground App with Background App

These are the available versions of this Windows 10 IoT Core sample.  

In both versions, the Background App currently toggles a GPIO pin.  If you are using a Dragonboard, 
you'll need to change LED_PIN in StartupTask.cpp (for C++) or StartupTask.cs (for C#) to a pin that 
exists on the Dragonboard (for example, the User LED 1: pin 21).  You can find a list of available
pins for the Dragonboard [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb).

## About this sample
If you want to create a solution that builds the foreground application and the background application into the same .APPX file it will require manual steps to combine the two projects.

### Steps

1. File>New>Project…
2. Create a new Blank App

![step 2](../../../Resources/images/ForegroundApp/step2.png)

3. Select desired target version and click OK when prompted for target version

![step 3](../../../Resources/images/ForegroundApp/step3.png)

4.	In Solution Explorer right-click on the solution and choose Add>New Project …

![step 4](../../../Resources/images/ForegroundApp/step4.png)

5.	Create a new Background Application

![step 5](../../../Resources/images/ForegroundApp/step5.png)

6.	Select desired target version and click OK when prompted for target version

![step 6](../../../Resources/images/ForegroundApp/step6.png)

7.	In Solution Explorer right-click on the background application Package.appxmanifest and choose View Code

![step 7](../../../Resources/images/ForegroundApp/step7.png)

8.	In Solution Explorer right-click on the foreground application Package.appxmanifest and choose View Code

![step 8](../../../Resources/images/ForegroundApp/step8.png)

9.	At the top of the foreground Package.appxmanifest add xmlns:iot="http://schemas.microsoft.com/appx/manifest/iot/windows10" and modify IgnorableNamespaces to include iot.

        <Package
        xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
        xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
        xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
        xmlns:iot="http://schemas.microsoft.com/appx/manifest/iot/windows10"
        IgnorableNamespaces="uap mp iot">

10.	Copy the <Extensions> from the Background Application project Package.appxmanifest  to the Foreground Application Package.appxmanifest.  It should look like this:

        <Applications>
        <Application Id="App"
            Executable="$targetnametoken$.exe"
            EntryPoint="MyForegroundApp.App">
            <uap:VisualElements
            DisplayName="MyForegroundApp"
            Square150x150Logo="Assets\Square150x150Logo.png"
            Square44x44Logo="Assets\Square44x44Logo.png"
            Description="MyForegroundApp"
            BackgroundColor="transparent">
            <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png"/>
            <uap:SplashScreen Image="Assets\SplashScreen.png" />
            </uap:VisualElements>
            <Extensions>
            <Extension Category="windows.backgroundTasks" EntryPoint="MyBackgroundApplication.StartupTask">
                <BackgroundTasks>
                <iot:Task Type="startup" />
                </BackgroundTasks>
            </Extension>
            </Extensions>
        </Application>
        </Applications>

11.	In Solution Explorer right-click on the Foreground Application References node and choose Add Reference…

![step 11](../../../Resources/images/ForegroundApp/step11.png)

12.	Add a project reference to the Background Application
 
![step 12](../../../Resources/images/ForegroundApp/step12.png)

13.	In Solution Explorer right-click the foreground application project and choose Unload Project, then right-click the background application project and choose Unload Project.

![step 13](../../../Resources/images/ForegroundApp/step13.png)

14.	In Solution Explorer right-click on the foreground application project and choose Edit MyForegroundApp.csproj and then right-click on the background application project and choose Edit MyBackgroundApp.csproj.
 
![step 14](../../../Resources/images/ForegroundApp/step14.png)

15.	In the background project file comment the following lines:

        <!--<PackageCertificateKeyFile>MyBackgroundApplication_TemporaryKey.pfx</PackageCertificateKeyFile>-->
        <!--<AppxPackage>true</AppxPackage>-->
        <!--<ContainsStartupTask>true</ContainsStartupTask>-->

16.	In the foreground project file add <ContainsStartupTask>true</ ContainsStartupTask> to the first PropertyGroup

        <PropertyGroup>
            <!-- snip -->
            <PackageCertificateKeyFile>MyForegroundApp_TemporaryKey.pfx</PackageCertificateKeyFile>
            <ContainsStartupTask>true</ContainsStartupTask>
        </PropertyGroup>

17.	In Solution Explorer right-click on each project and choose Reload Project

![step 17](../../../Resources/images/ForegroundApp/step17.png)

18.	In Solution Explorer delete Package.appxmanifest from the background application

![step 18](../../../Resources/images/ForegroundApp/step18.png)

19.	At this point the project should build (and run the implementation you have added to the foreground and background applications).
