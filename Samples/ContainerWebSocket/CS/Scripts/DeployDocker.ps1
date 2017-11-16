Set-StrictMode -Version 3
$ErrorActionPreference = "Stop"

### Variables

$DOCKER_VER = "17.09.0-ce"
$DOCKER_CE_URL = "https://download.docker.com/win/static/stable/x86_64/docker-" + $DOCKER_VER + ".zip"
$DOWNLOAD_PATH = $env:USERPROFILE + "\docker.zip"
$DOCKER_UNZIP_PATH = $env:USERPROFILE + "\docker"

try
{
    # Download Docker
    Write-Output "Downloading Docker $DOCKER_VER"
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $DOCKER_CE_URL -OutFile $DOWNLOAD_PATH -UseBasicParsing
    $ProgressPreference = 'Continue'

    Write-Output "Binplacing"

    # Unzip Docker

    Expand-Archive -Path $DOWNLOAD_PATH -DestinationPath $DOCKER_UNZIP_PATH -WarningAction 'SilentlyContinue' *>$null

    # Copy to SystemRoot
    $FilesToCopy = $DOCKER_UNZIP_PATH + "\docker\*"
    Copy-Item -Path $FilesToCopy -Destination $env:SystemRoot

    # Register and start docker daemon service
    Write-Output "Registering and starting daemon service"
    Invoke-Command -ScriptBlock {cmd /c "dockerd --register-service"} -ErrorVariable Errmsg 2>$null
    Invoke-Command -ScriptBlock {Start-Service docker} -ErrorVariable Errmsg *>$null

    # Workaround RS3 RTM HNS issue
    Set-ItemProperty -Path "HKLM:\System\CurrentControlSet\Control\WMI\Autologger\EventLog-System\{0c885e0d-6eb6-476c-a048-2457eed3a5c1}" -Name "Enabled" -Value 0
    Invoke-Command -ScriptBlock {cmd /c "tracelog.exe -disableex Eventlog-System -sessionguid #d2112be4-cd15-5a9c-e38f-080a207e08d5"} -ErrorVariable Errmsg 2>$null

    # Done
    Write-Output ""
    Write-Host "Note: You may encounter a one-time bluescreen crash during system reboot." -ForegroundColor "Yellow"
    Write-Host "      This is a known issue, and will be fixed in an upcoming OS update." -ForegroundColor "Yellow"
    Write-Output ""
    Write-Output ""
    Write-Host "Docker installed successfully." -ForegroundColor "Green"
    Write-Output ""
    Write-Output ""


}
catch
{
    Write-Output ""
    Write-Host "Docker install failed!" -ForegroundColor "Red"

}
finally
{

    # cleanup
    if ([System.IO.File]::Exists($DOWNLOAD_PATH))
    {
        Remove-Item $DOWNLOAD_PATH
    }

    if (Test-Path -Path $DOCKER_UNZIP_PATH)
    {
        Remove-Item -r $DOCKER_UNZIP_PATH -Force
    }
}