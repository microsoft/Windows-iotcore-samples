---
page_type: sample
urlFragment: SampleBridgeWmiScripts
languages:
- xml
products:
- windows
description: sample powershell scripts to call bridge WMI Shell Launcher node
---

# Shell Launcher V2 Bridge WMI Sample scripts

[ShellLauncherBridgeWmiHelpers.ps1](./ShellLauncherBridgeWmiHelpers.ps1) provides below functions
1. Set-ShellLauncherBridgeWmi, it takes a parameter FilePath to a raw config xml (not the escaped one) and configure Shell Launcher through bridge WMI 
2. Clear-ShellLauncherBridgeWmi, it clears shell launcher configuration using bridge WMI
3. Get-ShellLauncherBridgeWmi, it prints out the current shell launcher config xml if configured

To use the scripts,
1. Save the scripts file to your PC
2. Download SysInternals tools, run "psexec.exe -i -s powershell.exe" from elevated command prompt
3. In the powershell launched by psexec.exe, first import the scripts, notice the . command when importing the ps1 file
```
PS C:\Users\test> . .\ShellLauncherBridgeWmiHelpers.ps1
```
4. After importing, run the command Set-ShellLauncherBridgeWMI with FilePath pointing to a shell launcher config xml
```
PS C:\Users\test> Set-ShellLauncherBridgeWmi -FilePath .\ShellLauncher.xml
```
5. To clean up ShellLauncher using bridge WMI, run the other command Clear-ShellLauncherBridgeWMI

```
PS C:\Users\test> Clear-ShellLauncherBridgeWmi
```
6. To print current config xml, run the other command Get-ShellLauncherBridgeWMI

```
PS C:\Users\test> Get-ShellLauncherBridgeWmi
```
