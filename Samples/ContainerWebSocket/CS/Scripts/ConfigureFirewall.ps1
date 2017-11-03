write-host "netsh advfirewall firewall add rule name="Open Port 22122" dir=in action=allow protocol=TCP localport=22122"
netsh advfirewall firewall add rule name="Open Port 22122" dir=in action=allow protocol=TCP localport=22122

$IsIotCore = Test-Path "c:\windows\system32\IoTShell.exe"
if (!$IsIotCore) {
    $addresses = @(Get-NetIPAddress -InterfaceAlias "vEthernet*nat*" -AddressFamily IPv4)
    foreach ($address in $addresses) {
        $ipaddr = $address.IPAddress
        write-host "netsh http add urlacl url=http://${ipaddr}:22122/ user=${env:USERDOMAIN}\${env:USERNAME}"
        $result = (netsh http add urlacl url=http://${ipaddr}:22122/ user=${env:USERDOMAIN}\${env:USERNAME} 2>&1|out-string)
        if ($result.Contains("Error: 183")) {
            Write-Host "URL reservation already exists"
            Write-Host ""
        }
        else {
            Write-Host $result
        }
    }
}