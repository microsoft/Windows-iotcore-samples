<#
.SYNOPSIS
Convert json with macros to normal json

.INPUTS
Array of macrofile names and a template json file to process with the macrofiles

.OUTPUTS
normal json file with the macros recursively substituted for their values

.DESCRIPTION
This commandlet takes a list of json macro files of the form
{
    "macros" : [
        "jsonmacro1" : "value",
        "jsonmacro2" : {
            ... more complex json object...
        },
        ... other macros as above ...
    ]
}
and a json "template file" that is normal json file except that any string properties
with a value of the form "${<json macro name>}" will have that string property replace with
the json object value with the matching name.

.NOTES
The json macro files can themselves refer to any previously defined macro.
#>

[CmdletBinding()]
param(
    [parameter(Mandatory, Position = 0, ValueFromPipeline)] [string] $templatefile,
    [parameter(Mandatory)] [string[]] $macrofiles
)

<#
.NOTES
This is a slight variant on the suggested format-json fn from the github powershell issues thread 
about ugly formatting from convertto-json.
https://github.com/PowerShell/PowerShell/issues/2736
windows doesn't yet have the version of powershell core where this is built in.
#>
function Format-Json([Parameter(Mandatory, ValueFromPipeline)][String] $json, [int] $indentsize = 4) {
    $indent = 0;
    ($json -Split '\n' |
      % {
        if ($_ -match '[\}\]]') {
          # This line contains  ] or }, decrement the indentation level
          $indent--
        }
        $line = (' ' * $indent * $indentsize) + $_.TrimStart().Replace(':  ', ': ')
        if ($_ -match '[\{\[]') {
          # This line contains [ or {, increment the indentation level
          $indent++
        }
        $line
    }) -Join "`n"
  }

# add top level noteproperties from a pscustomobject to an ordered hashtable
# $h needs to be ordered but powershell won't allow [ordered] any where but literals
# and saying [hashtable] coerces to non ordered type 
function AddPSCustomToHash($h, [PSCustomObject] $o) {
    #write-host "h before" ($h | out-string)
    $o.psobject.properties |  %{
        #write-host "adding to h" $o."$($_.Name)".Gettype() ($o."$($_.Name)" | out-string)
        $h[$_.Name] = $o."$($_.Name)"
    } | out-null
    #write-host "h after" ($h | out-string)
}
function SubstituteString($macros, [string] $val)
{
    # match "${ anything except closing brace }"
    # braces aren't valid json identifier chars so shouldn't ever be in the
    # top level macro definition we're looking up anyway or the json shouldn't parse
    # in the first place
    if ($val -cmatch '^\${(?<macroname>[^}]+)}$') {
        #write-host "ismacro"
        $m = $macros[$Matches['macroname']]
        #write-host "ismacro: $val"
        #write-host "replacing: $m"
        $m
    } else {
        #write-host "notmacro"
        $val
    }
}
function SubstituteObject($macros, [PSCustomObject] $template )
{
    $tplobj = [ordered]@{}
    $retobj = [ordered]@{}
    #Write-Host "Adding to tpl hash"
    AddPSCustomToHash $tplobj $template
    #Write-Host "tpl hash now", ($tplobj | out-string)
    $tplobj.Keys | %{
        $newval = $null
        #Write-Host "Key $($_)" $tplobj[$_].GetType()
        if ($tplobj[$_].GetType() -eq [string]) {
                #Write-Host "substituting"
                $newval = SubstituteString $macros $tplobj[$_]
                #Write-host "substitute string returned" ($newval | out-string)
        } else {
# TODO: make sure this is the complete list of types that convertfrom-json can produce            
            if (
                $tplobj[$_].GetType() -eq [Int32] -or
                $tplobj[$_].GetType() -eq [Int64] -or
                $tplobj[$_].GetType() -eq [Boolean] -or
                $tplobj[$_].GetType() -eq [Decimal] -or
                $tplobj[$_].GetType() -eq [Double]
            )
             {
                #Write-Host "Using Primitive Value"          
                $newval = $tplobj[$_]
            } else {
                #Write-Host "Recursing"
                $newhash = SubstituteObject $macros $tplobj[$_]
                $newval = [PSCustomObject] $newhash
            }
        }
        $retobj[$_] = $newval
        #Write-host "retobj now " ($retobj | out-string)
    } | Out-Null
    #Write-host "final retobj " ($retobj | out-string)
    return [PSCustomObject] $retobj
}

set-strictmode -version 2

$macrohash = [ordered]@{}
# process the macrofile list in order given.  if more than 1 file subsequent files can use macros defined earlier
Write-Progress "Processing Macro Files"
$macrofiles | %{ 
    Write-Progress "Processing $($_)"
    #Write-Host "Processing $($_)"
    #Write-Host "macro hash now" ($macrohash | out-string)
    get-content $_ -erroraction stop | convertfrom-json | %{ 
            $_.Macros | %{ 
                #write-host "main macro hash before" ($macrohash | out-string)
                $newval = SubstituteObject $macrohash $_
                #write-host "adding to main macro hash " $newval.GetType() ($newval | out-string)
                AddPsCustomToHash $macrohash $newval
                #write-host "main macro hash after" ($macrohash | out-string)
            } 
    }
} | Out-Null

#Write-Host "Reading Template"
Write-Progress "Reading Template"
$template = get-content $templatefile | convertfrom-json

Write-Progress "Processing Template"
#Write-Host "Processing Template -- main macrohash now" ($macrohash | out-string)
$t = SubstituteObject $macrohash $template 
Write-Progress "Producing Output"
$t | convertto-json -depth 100 | format-json


