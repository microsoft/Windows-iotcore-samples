<#
.SYNOPSIS
generate a complete deployment file from standard macros and a template

.INPUTS
Hardware configuration macro and a template

.OUTPUTS
normal json file with the macros recursively substituted for their values

#>

[CmdletBinding()]
param(
    [parameter(Mandatory, Position = 0, ValueFromPipeline)] [string] $templatefile,
    [alias("hw")][string] $HardwareType="mbm"
)

if ($HardwareType -eq "mbm" -or $HardwareType -eq "minnowboard") {
    $HardwareType = "minnowboardmax"
}

$macros = @("$PSScriptRoot\urls.creds.macros.json")
# support different arch images coming from different locations in case of preview
$HardwareImageUrls = "$PSScriptRoot\urls.$HardwareType.creds.macros.json"
if (test-path $HardwareImageUrls) {
    $macros += @("$PSScriptRoot\urls.$HardwareType.creds.macros.json")
}
$macros += @("$PSScriptRoot\creds.macros.json")
if ((-not (test-path $templatefile)) -and (test-path "$PSScriptRoot\$templatefile")) {
    $templatefile = "$PSScriptRoot\$templatefile"
}

$macros += @("$PSScriptRoot\$Hardwaretype.macros.json")

#Write-Host $macros, $templatefile

&"$PSScriptRoot\convertfrom-macrofiedjson.ps1" $templatefile -macrofiles $macros 
