#Requires -Version 5
#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 5

function Get-WindowsBuild {
    (Get-Item "HKLM:\Software\Microsoft\Windows NT\CurrentVersion").GetValue("CurrentBuild")
}

function Get-WindowsEdition {
    (Get-Item "HKLM:\Software\Microsoft\Windows NT\CurrentVersion").GetValue("EditionID")
}

function Invoke-Native {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [String] $Command,

        [Switch] $Passthru
    )

    process {
        Write-Verbose "Executing native Windows command '$Command'..."
        $out = cmd /c "($Command) 2>&1" 2>&1 | Out-String
        Write-Verbose $out
        Write-Verbose "Exit code: $LASTEXITCODE"

        if ($LASTEXITCODE) {
            throw $out
        } elseif ($Passthru) {
            $out
        }
    }
}

$WindowsBuild = 16299
$RS3ProductName = "Windows Fall Creators Update a.k.a. RS3"
$RequiredFeatures = "Microsoft-Hyper-V", "Containers"
$DockerVersion = "17.09.0-ce"
$PythonVersion = "3.6.3"

<#
    Verify that the system's Windows build version is correct.
#>

if ((Get-WindowsBuild) -ne $WindowsBuild) {
    Write-Host ("Azure IoT Edge on Windows requires $RS3ProductName (build $WindowsBuild). " +
        "The current Windows build is $(Get-WindowsBuild). " +
        "Please ensure that the current Windows build is $WindowsBuild to run Azure IoT Edge on Windows.") `
        -ForegroundColor "Red"
    return
}

Write-Host "The current Windows build is $(Get-WindowsBuild)." -ForegroundColor "Green"

<#
    Ensure that Windows optional features required by Azure IoT Edge are enabled. These features are neither present
    nor needed on Windows IoT Core, so this step is skipped on that platform.
#>

if ((Get-WindowsEdition) -ne "IoTUAP") {
    Write-Progress -Activity "Enabling required features..."
    try {
        if ((Enable-WindowsOptionalFeature -FeatureName $RequiredFeatures -Online -All -NoRestart).RestartNeeded) {
            Write-Host ("A restart is required before completing the remainder of the installation. " +
                "This script must be rerun after the system has been restarted. " +
                "Would you like to restart now? (Y/N)") `
                -ForegroundColor "White"

            $response = Read-Host
            while(@("Y", "N") -notcontains $response) {
                Write-Host "Please enter Y or N to indicate whether the system should be restarted." `
                    -ForegroundColor "Yellow"
                $response = Read-Host
            }

            if ($response -eq "Y") {
                Restart-Computer -Confirm:$false
            }
            return
        }
    } catch {
        Write-Host ("Unable to enable a required Windows feature for Azure IoT Edge. " +
            "If the current system is running in a virtual machine, " +
            "then please ensure that nested virtualization is enabled. See " +
            "https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/nested-virtualization " +
            "for more information.") `
            -ForegroundColor "Red"
        return
    }
    Write-Progress -Activity "Enabling required features..." -Completed

    Write-Host "Required Windows features enabled successfully." -ForegroundColor "Green"
}

<#
    Install and initialize the correct version of Docker.
#>

if (-not (Get-Service | Where-Object {$_.Name -match "docker"})) {
    Write-Progress -Activity "Downloading Docker..."
    Invoke-WebRequest `
        -Uri "https://download.docker.com/win/static/stable/x86_64/docker-$DockerVersion.zip" `
        -OutFile (Join-Path $env:TEMP "docker.zip")
    Write-Progress -Activity "Downloading Docker..." -Completed

    Write-Progress -Activity "Installing Docker..."
    try {
        Expand-Archive -Path (Join-Path $env:TEMP "docker.zip") -DestinationPath $env:TEMP -Force
        Write-Progress -Activity "Installing Docker..."
        Join-Path $env:TEMP "docker" | Get-ChildItem | Copy-Item -Destination $env:SystemRoot -Force
        Remove-Item @((Join-Path $env:TEMP "docker.zip"), (Join-Path $env:TEMP "docker")) -Recurse -Force
        Invoke-Native "dockerd --register-service"
        Start-Service docker
    } finally {
        Remove-Item @((Join-Path $env:TEMP "docker.zip"), (Join-Path $env:TEMP "docker")) `
            -Recurse `
            -Force `
            -ErrorAction "SilentlyContinue"
    }
    Write-Progress -Activity "Installing Docker..." -Completed

    # Workaround for an RS3 RTM HNS issue.

    if ((Get-WindowsEdition) -eq "IoTUAP") {
        Set-ItemProperty `
            -Path "HKLM:\System\CurrentControlSet\Control\WMI\Autologger\EventLog-System\{0c885e0d-6eb6-476c-a048-2457eed3a5c1}" `
            -Name "Enabled" `
            -Value 0 `
            -Force
        Write-Host ("You may encounter a one-time bluescreen crash during system reboot. " +
            "This is a known issue, and will be fixed in an upcoming OS update.") `
            -ForegroundColor "Yellow"
    }

    Write-Host "Docker installed successfully." -ForegroundColor "Green"
} else {
    try {
        if (-not ((Invoke-Native "docker version" -Passthru) -match "Client:\s*Version:\s*(.*)\s*")) {
            throw "Unable to determine docker version."
        } elseif ($Matches[1] -lt $DockerVersion) {
            Write-Host ("Docker is already installed, but $DockerVersion or later is required. " +
                "Please uninstall Docker and rerun this script to install a compatible version.") `
                -ForegroundColor "Red"
            return
        } else {
            Write-Host ("Docker is already installed. Installation skipped. " +
                "Please ensure that Windows Containers are enabled if Docker for Windows is installed. ") `
                -ForegroundColor "Yellow"
        }
    } catch {
        Write-Host ("Docker is already installed, but its version was unable to be determined. " +
            "Please uninstall Docker and rerun this script to install a compatible version.") `
            -ForegroundColor "Red"
        return
    }
}
