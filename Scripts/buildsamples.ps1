[CmdletBinding()]
Param(
    [switch]$Quick = $false,
    [switch]$Clean = $false,
    [switch]$ScanOnly = $false,
    [switch]$NoScan = $false,
    [switch]$ErrorOnly = $false
)

If ($PSBoundParameters['Debug']) {
    $DebugPreference = 'Continue'
}
$ErrorActionPreference = "Stop"
    
Write-Debug "PSScriptRoot = $PSScriptRoot"

$LogsPath = "$PSScriptRoot\logs"
$NUGET = "$PsScriptRoot\nuget.exe"

Write-Debug "LogsPath = $LogsPath"
if ($Clean) {
    Remove-Item $LogsPath -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $NUGET -Force -ErrorAction SilentlyContinue
    return
}

if (!(Test-Path $LogsPath)) {
    mkdir $LogsPath
}

$blocked_Arm = @(
)

$blocked_x86 = @(
)

$blocked_x64 = @(
    "BlinkyApp.sln",
    "OnboardingClient.sln",
    "OnboardingServer.sln"
)

$allowed_AnyCpu = @(
)

$blocked_always = @(
    "CompanionAppClient.sln",
    "ConsoleDotNetCoreWinML.sln",
    "ContainerWebSocket.sln"
    "CustomAdapter.sln",
    "EdgeModulesCS.sln",
    "gpiokmdfdemo.sln",
    "InternetRadioDevice.sln",
    "IoTConnectorClient.sln",
    "IoTOnboarding.sln",
    "NodeBlinkyServer.sln",
    "NodeJsBlinky.sln",
    "NTServiceRpc.sln",
    "OpenCVExample.sln",
    "ReadDeviceToCloudMessages.sln",
    "VirtualAudioMicArray.sln",  # requires device kit
    "XamarinIoTViewer.sln"  # not sure what this requires
)

$blocked_endswith = @(
    "node.js\BlinkyClient.sln",
    "node.js\BlinkyClient.sln"
)

$drivers = @(
)

function PressAnyKey() {
    Write-Host "Press any key to continue ..."
    $x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

function EnsureNugetExe() {
    Write-Debug "nuget = $NUGET"
    if (!(Test-Path $NUGET)) {
        Invoke-WebRequest -Uri https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile $NUGET
    }
}

function TestFullPathEndsWith($path, $list) {
    foreach ($s in $list) {
        if ($path.EndsWith($s)) {
            return $true;
        }
    }
    return $false;
}

function restoreConfigs($filename, $solutionDir) {
    write-host -ForegroundColor Cyan "nuget.exe restore $filename"
    &"$NUGET" restore $filename
	
    $configFiles = Get-ChildItem packages.config -Recurse
    foreach ($c in $configFiles) {
        $fullname = $c.FullName
        write-host -ForegroundColor Cyan "nuget.exe restore $fullname -SolutionDirectory $path"
        &"$NUGET" restore $fullname
    }
}

function SkipThisFile($file) {
    $filename = $file.Name

    if (TestFullPathEndsWith $file.FullName $blocked_endswith) {
        return $true;
    }
    if (TestFullPathEndsWith $file.FullName $drivers) {
        return $true;
    }
    if ($blocked_always.Contains($filename)) {
        return $true;
    }
    if ($platform -eq "ARM") {
        if ($blocked_Arm.Contains($filename)) {
            return $true;
        }
    }
    if ($platform -eq "x86") {
        if ($blocked_x86.Contains($filename)) {
            return $true;
        }
    }
    if ($platform -eq "x64") {
        if ($blocked_x64.Contains($filename)) {
            return $true;
        }
    }
    if ($platform -eq '"Any CPU"') {
        if (!($allowed_AnyCpu.Contains($filename))) {
            return $true;
        }
    }
    return $false;
}

function restoreNuget($file) {
    if (SkipThisFile $file) {
        return
    }

    $filename = $file.Name
    $path = split-path $file.FullName -Parent
    Write-Host -ForegroundColor Cyan "Found $file"
    #pushd $path
    #restoreConfigs "packages.config" $path
    #restoreConfigs "project.json" $path
    #popd
    & "$NUGET" restore $file.FullName
}

function Get-MSBuild-Path {

    $vs14key = "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0"
    $vs15key = "HKLM:\SOFTWARE\wow6432node\Microsoft\VisualStudio\SxS\VS7"

    $msbuildPath = ""

    if (Test-Path $vs14key) {
        $key = Get-ItemProperty $vs14key
        $subkey = $key.MSBuildToolsPath
        if ($subkey) {
            $msbuildPath = Join-Path $subkey "msbuild.exe"
        }
    }

    if (Test-Path $vs15key) {
        $key = Get-ItemProperty $vs15key
        $subkey = $key."15.0"
        if ($subkey) {
            $msbuildPath = Join-Path $subkey "MSBuild\15.0\bin\msbuild.exe"
        }
    }

    return $msbuildPath

}

function getLogStatus($logPath) {
    if (!(Test-Path $logPath)) { return "none" }

    $succeeded = Get-ChildItem $logPath | select-string "Build [sf][kua][~ ]*"
    $out = $succeeded -split ":[0-9]*:"
    if ($out[1].Equals("Build FAILED.")) {
        return "fail"
    }
    elseif ($out[1].Equals("Build skipped.")) {
        return "skip"
    } 
    else {
        return "success"
    }
}

function buildSolution($file, $config, $platform, $logPlatform) {
    $msbuildpath = Get-MSBuild-Path
    $filename = $file.Name
	
    $language = ""
    #write-host $file.FullName.ToLower()
    if ($file.FullName.ToLower().Contains("cpp")) {
        $language = ".CPP";
    }
    elseif ($file.FullName.ToLower().Contains("cs")) {
        $language = ".CS";
    }
    elseif ($file.FullName.ToLower().Contains("vb")) {
        $language = ".VB";
    }
    elseif ($file.FullName.ToLower().Contains("node.js")) {
        $language = ".Node-js";
    }
    elseif ($file.FullName.ToLower().Contains("python")) {
        $language = ".Python";
    }
    
    #write-host -ForegroundColor Cyan "$LogsPath\$filename.$config$language.$logPlatform.log"
    $logPath = "$LogsPath\$filename.$config$language.$logPlatform.log"
    
    $status = getLogStatus($logPath)
    if (($status -eq "success") -or ($status -eq "skip")) {
        Write-Host "Skipping $logPath because a non-failing log file exists"
        return 
    }

    if (Test-Path $logPath ) { del $logPath }

    if (SkipThisFile $file) {
        write-host "skipping $filename $config $platform"
        Add-Content $logPath "Build skipped."
        return;
    }
	
    $logCommand = "/logger:FileLogger,Microsoft.Build.Engine;logfile=$logPath"
    write-host -ForegroundColor Cyan "${msbuildpath} $file `"/t:clean;restore;build /verbosity:normal`" `"/p:Configuration=$config`" `"/p:Platform=$platform`" $logCommand"
    &"$msbuildpath" $file "/t:clean;restore;build" /verbosity:normal /p:Configuration=$config /p:Platform=$platform ${logCommand}

    #$errors = findstr "Error\(s\)" "$logPath"
    #write-host -ForegroundColor Red $errors
}

function BuildAll() {
    $files = Get-ChildItem "*.sln" -Recurse

    foreach ($f in $files) {
        restoreNuget $f
        buildSolution $f "Release" "x86" "x86"
        if (!$Quick) {
            buildSolution $f "Debug" "x86" "x86"
            buildSolution $f "Release" "x64" "x64"
            buildSolution $f "Debug" "x64" "x64"
            buildSolution $f "Release" "ARM" "ARM"
            buildSolution $f "Debug" "ARM" "ARM"
            buildSolution $f "Release" '"Any CPU"' "AnyCPU"
            buildSolution $f "Debug" '"Any CPU"' "AnyCPU"
        }
    }   
}

function ScanLogs() {
    $fail = 0
    $skip = 0
    $pass = 0
    $succeeded = Get-ChildItem -Recurse -Path $LogsPath -Include *.log | select-string "Build [sf][kua][~ ]*"
    foreach ($bs in $succeeded) {
        $out = $bs -split ":[0-9]*:"
        if ($out[1].Equals("Build FAILED.")) {
            $fail = $fail + 1
        }
        elseif ($out[1].Equals("Build skipped.")) {
            $skip = $skip + 1
        } 
        else {
            $pass = $pass + 1
            if (!$ErrorOnly) {
                write-host -ForegroundColor Green $out[0] - $out[1]
            }
        }
    }
    foreach ($bs in $succeeded) {
        $out = $bs -split ":[0-9]*:"
        if ($out[1].Equals("Build skipped.")) {
            if (!$ErrorOnly) {
                write-host -ForegroundColor Cyan $out[0] - $out[1]
            }
        } 
    }
    foreach ($bs in $succeeded) {
        $out = $bs -split ":[0-9]*:"
        if ($out[1].Equals("Build FAILED.")) {
            write-host -ForegroundColor Red $out[0] - $out[1]
        }
    }
    write-host "================================================================================"
    write-host "Build succeeded: $pass"
    write-host "Build skipped: $skip"
    write-host "Build failed: $fail"
    write-host "================================================================================"
}


$stopwatch = [Diagnostics.StopWatch]::StartNew()

if (!$ScanOnly) {
    EnsureNugetExe
    BuildAll
}

if (!$NoScan) {
    ScanLogs
}

$stopwatch.Stop()
$t = $stopwatch.Elapsed
Write-Host $([string]::Format("Elapsed time = {0:d2}:{1:d2}:{2:d2}", $t.hours, $t.minutes, $t.seconds))
