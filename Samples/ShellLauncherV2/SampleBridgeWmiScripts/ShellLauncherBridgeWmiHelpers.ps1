$NameSpace = "root\cimv2\mdm\dmmap"
$Class = "MDM_AssignedAccess"

function Get-AssignedAccessCspBridgeWmi
{
    return Get-CimInstance -Namespace $NameSpace -ClassName $Class
}

function Set-ShellLauncherBridgeWMI
{
    param([Parameter(Mandatory=$True)][String] $FilePath)

    $Xml = Get-Content -Path $FilePath
    $EscapedXml = [System.Security.SecurityElement]::Escape($Xml)
    $AssignedAccessCsp = Get-AssignedAccessCspBridgeWmi
    $AssignedAccessCsp.ShellLauncher = $EscapedXml
    Set-CimInstance -CimInstance $AssignedAccessCsp
    
    # get a new instance and print the value
    (Get-AssignedAccessCspBridgeWmi).ShellLauncher
}

function Clear-ShellLauncherBridgeWMI
{
    $AssignedAccessCsp = Get-AssignedAccessCspBridgeWmi
    $AssignedAccessCsp.ShellLauncher = $NULL
    Set-CimInstance -CimInstance $AssignedAccessCsp
}

function Get-ShellLauncherBridgeWMI
{
    (Get-AssignedAccessCspBridgeWmi).ShellLauncher
}
