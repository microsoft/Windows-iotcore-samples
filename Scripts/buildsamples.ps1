# Param statement must be first non-comment, non-blank line in the script
Param(
    $LogsPath="g:\logs"
    )

$blocked_Arm = @(
    "ReadDeviceToCloudMessages.sln",
    "TpmDeviceSample.sln",
    "BACnetAdapter.sln"
)

$blocked_x86 = @(
    "ReadDeviceToCloudMessages.sln",
    "TpmDeviceSample.sln",
    "ArduinoLibraryLcdDisplay.sln"
)

$blocked_x64 = @(
    "ReadDeviceToCloudMessages.sln",
    "TpmDeviceSample.sln",
    "ArduinoLibraryLcdDisplay.sln",
    "BlinkyApp.sln"
)

$allowed_AnyCpu = @(
    "ReadDeviceToCloudMessages.sln",
    "TpmDeviceSample.sln"
)

$blocked_always = @(
    "AllJoyn.JS.sln",
    "BACnet Stack Development.sln",
    "bacnet.sln",
    "Microsoft Visual Studio 2005.sln",
    "MyLivingRoom.sln",
    "ptransfer.sln",
    "readrange.sln",
    "CompanionAppClient.sln",
    "CustomAdapter.sln",
    "DeviceSystemBridgeTemplate_2015.sln",
    "DeviceSystemBridgeTemplate_2017.sln",
    "GoPiGoXboxWebService.sln",
    "hidapi.sln",
    "IoTConnector.sln",
    "IoTConnectorClient.sln",
    "XamarinIoTViewer.sln",
    "PythonAccelerometer.sln",
    "PythonBlinkyHeadless.sln",
    "PythonBlinkyServer.sln",
    "PythonWeatherStation.sln",
    "NodeBlinkyServer.sln",
    "NodeJsBlinky.sln",
    "NodeWeatherStation.sln",
    "IoTOnboarding.sln",
    "ManagedCustomAdapter.sln",
    "MinOZW.sln",
    "OpenCVExample.sln",
    "WindowsIotCoreTemplatesDev14.sln",
    "WindowsIotCoreTemplatesDev15.sln"
)

$blocked_endswith = @(
    "node.js\BlinkyClient.sln",
    "node.js\BlinkyClient.sln"
)

$drivers = @(
    "gpiokmdfdemo.sln",
    "DriverSamples\consoleapp\BlinkyApp\BlinkyApp.sln",
    "HidInjector.sln",
    "VirtualAudioMicArray.sln"
)

function PressAnyKey()
{
    Write-Host "Press any key to continue ..."
    $x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

function TestFullPathEndsWith($path, $list)
{
    foreach($s in $list)
    {
        if($path.EndsWith($s))
        {
            return $true;
        }
    }
    return $false;
}

function restoreConfigs($filename, $solutionDir)
{
    $configFiles = Get-ChildItem packages.config -Recurse
    foreach($c in $configFiles)
    {
        $fullname = $c.FullName
        write-host -ForegroundColor Cyan "fullname=$fullname"
        nuget restore $fullname "-SolutionDirectory" $path
    }
}

function SkipThisFile($file)
{
    $filename = $file.Name

    if (TestFullPathEndsWith $file.FullName $blocked_endswith)
    {
        return $true;
    }
    if (TestFullPathEndsWith $file.FullName $drivers)
    {
        return $true;
    }
    if ($blocked_always.Contains($filename))
    {
        return $true;
    }
    if ($platform -eq "ARM")
    {
        if ($blocked_Arm.Contains($filename))
        {
            return $true;
        }
    }
    if ($platform -eq "x86")
    {
        if ($blocked_x86.Contains($filename))
        {
            return $true;
        }
    }
    if ($platform -eq "x64")
    {
        if ($blocked_x64.Contains($filename))
        {
            return $true;
        }
    }
    if ($platform -eq '"Any CPU"')
    {
        if (!($allowed_AnyCpu.Contains($filename)))
        {
            return $true;
        }
    }
    return $false;
}

function restoreNuget($file)
{
    if (SkipThisFile $file)
    {
        return
    }

    $filename = $file.Name
    $path = split-path $file.FullName -Parent
    Write-Host -ForegroundColor Cyan "Found $file"
    pushd $path
    restoreConfigs "packages.config" $path
    restoreConfigs "project.json" $path
    popd
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
            $msbuildPath = Join-Path $subkey "MSBuild\15.0\bin\amd64\msbuild.exe"
        }
    }

    return $msbuildPath

}

function buildSolution($file, $config, $platform, $logPlatform)
{
	$msbuildpath = Get-MSBuild-Path
    $filename = $file.Name
	
	$language = ""
	#write-host $file.FullName.ToLower()
	if ($file.FullName.ToLower().Contains("cpp")) {
		$language = ".CPP";
	} elseif ($file.FullName.ToLower().Contains("cs")) {
		$language = ".CS";
	} elseif ($file.FullName.ToLower().Contains("node.js")) {
		$language = ".Node-js";
	} elseif ($file.FullName.ToLower().Contains("python")) {
		$language = ".Python";
	}
	
    #write-host -ForegroundColor Cyan "$LogsPath\$filename.$config$language.$logPlatform.log"
    $logPath = "$LogsPath\$filename.$config$language.$logPlatform.log"
    if (Test-Path $logPath ){ del $logPath }

    if (SkipThisFile $file)
    {
         write-host "skipping $filename $config $platform"
		 Add-Content $logPath "Build skipped."
         return;
    }
	
    $logCommand = "/logger:FileLogger,Microsoft.Build.Engine;logfile=$LogsPath\$filename.$config$language.$logPlatform.log"
    write-host -ForegroundColor Cyan "${msbuildpath} $file `"/t:clean;restore;build /verbosity:normal`" `"/p:Configuration=$config`" `"/p:Platform=$platform`" $logCommand"
    &"$msbuildpath" $file "/t:clean;restore;build" /verbosity:normal /p:Configuration=$config /p:Platform=$platform ${logCommand}

    #$errors = findstr "Error\(s\)" "$LogsPath\$filename.$config$language.$platform.log"
    #write-host -ForegroundColor Red $errors
}

# del $LogsPath\*
$files = Get-ChildItem "*.sln" -Recurse
foreach ($f in $files)
{
    restoreNuget $f
}

foreach ($f in $files)
{
    buildSolution $f "Release" "x86" "x86"
    buildSolution $f "Release" "x64" "x64"
    buildSolution $f "Release" "ARM" "ARM"
    buildSolution $f "Release" '"Any CPU"' "AnyCPU"
}

$succeeded = Get-ChildItem -Recurse -Path $LogsPath -Include *.log | select-string "Build [sf][kua][~ ]*"
foreach ($bs in $succeeded) {
	$out = $bs -split ":[0-9]*:"
	$color = "Green"
	if ($out[1].Equals("Build FAILED.")) {
		$color = "Red"
	} elseif ($out[1].Equals("Build skipped.")) {
		$color = "Cyan"
	} 
	write-host -ForegroundColor $color $out[0] - $out[1]
}
