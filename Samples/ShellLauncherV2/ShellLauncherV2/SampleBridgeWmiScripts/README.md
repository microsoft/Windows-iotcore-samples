--- 
topic: sample
urlFragment: SampleBridgeWmiScripts
languages:
-xml
products:
-windows
description: sample powershell scripts to call bridge WMI Shell Launcher node
---

# Shell Launcher V2 Bridge WMI Sample scripts

* [ShellLauncherBridgeWmiHelpers.ps1](./ShellLauncherBridgeWmiHelpers.ps1) this script provides two functions
1. SetShellLauncherBridgeWmi, it takes a parameter FilePath to a raw config xml (not the escaped one) and configure Shell Launcher through bridge WMI 
2. ClearShellLauncherBridgeWmi, it clears shell launcher configuration using bridge WMI

To use the scripts,
1. Save the scripts file to your PC
2. Download SysInternals tools, run "psexec.exe -i -s powershell.exe" from elevated command prompt
3. In the powershell launched by psexec.exe, first import the scripts, be aware the . command when importing the ps1 file
```
PS C:\Users\test> . .\ShellLauncherBridgeWmiHelpers.ps1
```
4. After importing, run the command SetShellLauncherBridgeWMI with FilePath pointing to a shell launcher config xml
```
PS C:\Users\test> SetShellLauncherBridgeWmi -FilePath .\ShellLauncher.xml
```
5. To clean up ShellLauncher using bridge WMI, run the other command ClearShellLauncherBridgeWMI

```
PS C:\Users\test> ClearShellLauncherBridgeWmi
```
