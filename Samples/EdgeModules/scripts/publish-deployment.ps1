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
    [parameter(Mandatory)] [alias("hw")][string[]] $Hardwaremacrofile
)

$macros = @("$PSScriptRoot\urls.creds.macros.json", "$PSScriptRoot\creds.macros.json")
if ((-not (test-path $templatefile)) -and (test-path "$PSScriptRoot\$templatefile")) {
    $templatefile = "$PSScriptRoot\$templatefile"
}
if ((-not (test-path $Hardwaremacrofile)) -and (test-path "$PSScriptRoot\$Hardwaremacrofile")) {
    $macros += @("$PSScriptRoot\$Hardwaremacrofile")
} else {
    $macros += @($Hardwaremacrofile)
}

&"$PSScriptRoot\convertfrom-macrofiedjson.ps1" $templatefile -macrofiles $macros 
