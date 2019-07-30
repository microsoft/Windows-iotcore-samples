---
page_type: sample
urlFragment: SampleConfigXmls
languages:
- xml
products:
- windows
description: sample shell launcher configuration xmls using Assigned Access CSP
---

# Shell Launcher V2 configuration xml samples

See more information at [ShellLauncher node on Assigned Access CSP](https://docs.microsoft.com/en-us/windows/client-management/mdm/assignedaccess-csp)

* [ShellLauncherAutoLogonUwp.xml](./ShellLauncherAutoLogonUwp.xml), this sample shows how to create an auto-logon account using Shell Launcher V2, and assign an UWP app for this account as shell
* [ShellLauncherAzureADMultiUser.xml](./ShellLauncherAzureADMultiUser.xml), this sample shows how to configure multiple AzureAD accounts to different shell
* [ShellLauncherDefaultOnlyUwp.xml](./ShellLauncherDefaultOnlyUwp.xml), this sample shows how to configure only one default profile for everyone, with empty Configs. Everyone would log into the same UWP Shell app
* [ShellLauncherSid.xml](./ShellLauncherSid.xml), this sample shows how to configure a SID for Shell Launcher. The SID can be either user sid, or local group sid, or AD group sid
* [ShellLauncherConfiguration_Demo.syncml](./ShellLauncherConfiguration_Demo.syncml), this sample shows what the SyncML file would look like, when using ShellLauncherV2 and Assigned Access CSP. This is the payload when MDM server sends the configuration to client.

## Xml Namespace

In order to invoke Shell Launcher V2, instead of legacy Shell Launcher (which uses eshell.exe), you must specify the v2 namespace http://schemas.microsoft.com/ShellLauncher/2019/Configuration in the xml. 

* When you want to use an UWP app as shell, use the v2 attribute AppType (v2:AppType="UWP")
* The V2 namespace also provides a new switch to force all windows full screen, V2:AllAppsFullScreen="true"

For the complete XSD, please refer to the CSP link above

## How to get group sid

To get local group sid, replace Guests to the group you need
```
PS C:\Users\test> $group = Get-LocalGroup -Name Guests
PS C:\Users\test> $group.SID

BinaryLength AccountDomainSid Value
------------ ---------------- -----
          16                  S-1-5-32-546
```

To get AD group sid, replace MyADGroup to the group you need, take the Value part
```
PS C:\Users\test> $AdGroup = New-Object System.Security.Principal.NTAccount("MyADGroup")
PS C:\Users\test> $AdGroupSid = $AdGroup.Translate([System.Security.Principal.SecurityIdentifier])
PS C:\Users\test> $AdGroupSid

BinaryLength AccountDomainSid                          Value
------------ ----------------                          -----
          28 S-1-5-21-2127521184-1604012920-1887927527 S-1-5-21-2127521184-1604012920-1887927527-32599559
```
