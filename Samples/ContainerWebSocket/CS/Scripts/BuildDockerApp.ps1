pushd $PSScriptRoot\..\ContainerApp

Write-Host -ForegroundColor Cyan "Remove published directory if it exists"
rd .\published -Recurse -Force -ErrorAction SilentlyContinue

Write-Host -ForegroundColor Cyan "Publishing self-contained DockerApp"
dotnet publish -o published --self-contained -r win10-x64

Write-Host -ForegroundColor Cyan "Stop and remove docker container if it already exists"
docker kill (docker ps --filter "ancestor=containerapp" -q) 2>&1 |out-null
docker rmi (docker images --filter=reference='containerapp' -q) -f 2>&1 |out-null

Write-Host -ForegroundColor Cyan "Prune dangling containers"
docker container prune -f

Write-Host -ForegroundColor Cyan "Building docker container"
docker build . -t containerapp:latest

popd