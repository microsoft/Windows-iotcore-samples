# remove all untagged images
param([switch] $force)
[string]$forcearg=""
if ($force) {
    $forcearg = "-f"
}
docker image ls | select-object -skip 1 | %{
    [string[]]$image = [Regex]::split($_, "\s+") | select-object -first 3
    if ($image[1] -eq '<none>') {
        write-host "docker rmi $forcearg $($image[2])"
        docker rmi $forcearg $image[2]
    }
} | out-null