--- 
topic: sample
urlFragment: SampleConfigXmls
languages:
-xml
products:
-windows
description: sample shell launcher configuration xmls using Assigned Access CSP
---

# Shell Launcher V2 configuration xml samples

See more information at [ShellLauncher node on Assigned Access CSP](https://docs.microsoft.com/en-us/windows/client-management/mdm/assignedaccess-csp)

* ShellLauncherAutoLogonUwp.xml, this sample shows how to create an auto-logon account using Shell Launcher V2, and assign an UWP app for this account as shell
* ShellLauncherAzureADMultiUser.xml, this sample shows how to configure multiple AzureAD accounts to different shell
* ShellLauncherConfiguration_Demo.syncml, this sample shows what the SyncML file would look like, when using ShellLauncherV2 and Assigned Access CSP
* ShellLauncherDefaultOnlyUwp.xml, this sample shows how to configure only one default profile for everyone, with empty Configs. Everyone would log into the same UWP Shell app

## Xml Namespace

In order to invoke Shell Launcher V2, instead of legacy Shell Launcher (which uses eshell.exe), you must specify the v2 namespace http://schemas.microsoft.com/ShellLauncher/2019/Configuration in the xml. 

* When you want to use an UWP app as shell, use the v2 attribute AppType (v2:AppType="UWP")
* The V2 namespace also provides a new switch to force all windows full screen, V2:AllAppsFullScreen="true"

For the complete XSD, please refer to the CSP link above
