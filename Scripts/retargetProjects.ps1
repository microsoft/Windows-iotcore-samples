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

    $projectJson = "$folder\project.json"
    Write-Debug $projectJson
    if (Test-Path $projectJson) {
        Write-Host "Removing $projectJson"
        Remove-Item $projectJson
    }

    $xmlns = 'http://schemas.microsoft.com/developer/msbuild/2003'
    [System.Xml.XmlNamespaceManager] $nsmgr = $proj.NameTable
    $nsmgr.AddNamespace('ns', $xmlns)

    $node = $proj.SelectSingleNode("//ns:TargetPlatformVersion", $nsmgr)
    if ($node.'#text') {
        $node.'#text'= "10.0.17763.0"
        $find_rid = $proj.SelectSingleNode("//ns:RuntimeIdentifiers", $nsmgr)
        if (!$find_rid)
        {
            $parent = $node.ParentNode
            $rid = $proj.CreateElement("RuntimeIdentifiers", $xmlns)
            $child = $parent.AppendChild($rid)
            $child.InnerXml = "win10-arm;win10-arm-aot;win10-x86;win10-x86-aot;win10-x64;win10-x64-aot"
        }
    }

    $cpp_XPath = "//ns:WindowsTargetPlatformVersion"
    $wtpv = $proj.SelectSingleNode($cpp_XPath, $nsmgr)
    if ($wtpv.'#text') {
        $wtpv.'#text'= "10.0.17763.0"
    }

    $sdkreference = "//ns:SDKReference"
    $node = $proj.SelectSingleNode($sdkreference, $nsmgr)
    if ($node)
    {
        write-host "found sdkreference"
        write-host $node.InnerXml
        $itemGroupParent = $node.ParentNode.ParentNode
        $itemGroupParent.RemoveChild($node.ParentNode)
    }

    $noneRef = "//ns:None[@Include='project.json']"
    $none = $proj.SelectSingleNode($noneRef, $nsmgr)
    if ($none)
    {
        write-host "found include project.json"
        write-host $none.InnerXml
        if ($none.Attributes['Include']) {
            $parent = $none.ParentNode.ParentNode
            $x = $parent.RemoveChild($none.ParentNode)
        }
    }

    if ($projectPath.Name.EndsWith(".csproj")) {
        $testItem = $proj.SelectSingleNode("//ns:PackageReference[@Include='Microsoft.NETCore.UniversalWindowsPlatform']", $nsmgr)
        if (!$testItem)
        {
            $items = $proj.SelectNodes("//ns:ItemGroup", $nsmgr)       
            $el = $proj.CreateElement("ItemGroup", $xmlns)
            $itemGroup = $proj.Project.InsertAfter($el, $items[$items.Count - 1])
            $el = $proj.CreateElement("PackageReference", $xmlns)
            $packageReference = $itemGroup.AppendChild($el)
            $packageReference.SetAttribute("Include", "Microsoft.NETCore.UniversalWindowsPlatform")
            $el = $proj.CreateElement("Version", $xmlns)
            $version = $packageReference.AppendChild($el)
            $version.InnerXml = "5.0.0"
        }
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
