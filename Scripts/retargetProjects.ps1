[CmdletBinding()]
Param(
    [switch]$Quick = $false,
    [switch]$Trace = $false
)

If ($PSBoundParameters['Debug']) {
    $DebugPreference = 'Continue'
}

if ($Trace) {
    Set-PSDebug -Trace 1
}
else {
    Set-PSDebug -Trace 0
}
$ErrorActionPreference = "Stop"
    
Write-Debug "PSScriptRoot = $PSScriptRoot"

$LogsPath = "$PSScriptRoot\logs"
Write-Debug "LogsPath = $LogsPath"
if (!(Test-Path $LogsPath)) {
    mkdir $LogsPath
}

function retargetSingleProject($projectPath, $folder)
{
    Write-Host "Retargeting Project = $projectPath"
    $proj = [xml](Get-Content $projectPath)

    $xmlns = 'http://schemas.microsoft.com/developer/msbuild/2003'
    [System.Xml.XmlNamespaceManager] $nsmgr = $proj.NameTable
    $nsmgr.AddNamespace('ns', $xmlns)

    # replace version in .csproj files
    $node = $proj.SelectSingleNode("//ns:TargetPlatformVersion", $nsmgr)
    if ($node.'#text') {
        $node.'#text'= "10.0.17763.0"
    }

    # replace version in .vcxproj files
    $cpp_XPath = "//ns:WindowsTargetPlatformVersion"
    $wtpv = $proj.SelectSingleNode($cpp_XPath, $nsmgr)
    if ($wtpv.'#text') {
        $wtpv.'#text'= "10.0.17763.0"
    }

    $proj.Save($projectPath)
}

$files = Get-ChildItem "*.vcxproj" -Recurse
foreach ($f in $files) {
    retargetSingleProject $f $f.Directory.FullName
}

$files = Get-ChildItem "*.csproj" -Recurse
foreach ($f in $files) {
    $result = retargetSingleProject $f $f.Directory.FullName
}
