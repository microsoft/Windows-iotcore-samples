# Save this ps1 file to your PC
# Download SysInternals tools, run "psexec.exe -i -s powershell.exe" from elevated command prompt

# In the powershell launched by psexec.exe, first load the ps1 file, be aware the . command when importing the ps1 file
# PS C:\Users\test> . .\ShellLauncherBridgeWmi.ps1

# After importing, run the command SetShellLauncherBridgeWMI with FilePath pointing to a shell launcher config xml
# this config xml is raw xml that's NOT escaped
# PS C:\Users\test> SetShellLauncherBridgeWMI -FilePath .\ShellLauncher.xml

# To clean up ShellLauncher using bridge WMI, run the other command ClearShellLauncherBridgeWMI

function SetShellLauncherBridgeWMI
{
    param([Parameter(Mandatory=$True)][String] $FilePath)

    $Xml = Get-Content -Path $FilePath
    $EscapedXml = [System.Security.SecurityElement]::Escape($Xml)
    $AssignedAccessCsp = Get-CimInstance -Namespace "root\cimv2\mdm\dmmap" -ClassName "MDM_AssignedAccess"
    $AssignedAccessCsp.ShellLauncher = $EscapedXml
    Set-CimInstance -CimInstance $AssignedAccessCsp
    
    $AssignedAccessCsp1 = Get-CimInstance -Namespace "root\cimv2\mdm\dmmap" -ClassName "MDM_AssignedAccess"
    $AssignedAccessCsp1.ShellLauncher
}

function ClearShellLauncherBridgeWMI
{
    $AssignedAccessCsp = Get-CimInstance -Namespace "root\cimv2\mdm\dmmap" -ClassName "MDM_AssignedAccess"
    $AssignedAccessCsp.ShellLauncher = $NULL
    Set-CimInstance -CimInstance $AssignedAccessCsp
}
