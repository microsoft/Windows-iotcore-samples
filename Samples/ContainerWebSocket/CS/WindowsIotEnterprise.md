# Using WebSockets to communicate with Docker containers on Windows IoT Enterprise

## Before You Begin

This sample requires 64-bit Windows IoT Enterprise.

The following Visual Studio 2017 project types and templates must be installed
* Universal Windows Platform development tools
* .Net Core 2.0 development tools
* [**Windows IoT Core Project Templates**](https://marketplace.visualstudio.com/items?itemName=MicrosoftIoT.WindowsIoTCoreProjectTemplatesforVS15).

## Install Docker on Windows IoT Enterprise

Run ./Scripts/ProvisionDocker.ps1
This script will

1. Enable Hyper-V Windows feature.
2. Enable Containers Windows feature.
3. [Install Docker for Windows](https://docs.docker.com/docker-for-windows/)

If you already have Docker for Windows installed you will need to switch to Windows containers.
You can do this by right-clicking on the Docker whale icon on the Windows taskbar, and choosing "Switch to Windows Containers..."

## Build and run the sample WebSocket server
1. Open websocket.sln in Visual Studio 2017
2. Set ConsoleServer as the startup project by right clicking on the project in the Solution Explorer and choosing **Set as Startup Project**
3. On the Debug toolbar choose **Debug**, **x64**.
4. On the Debug toolbar choose the Start button (F5).


## build and run the sample WebSocket client
Launch an elevated Powershell command prompt by running as Administrator. In the elevated PowerShell window run the following commands:

```
Set-ExecutionPolicy RemoteSigned
cd .\Scripts
.\ConfigureFirewall.psq
docker pull microsoft/nanoserver:1709
docker tag microsoft/nanoserver:1709 microsoft/nanoserver:latest
docker images
```

When you run **docker images** the output should look like this:

```
REPOSITORY             TAG                 IMAGE ID            CREATED             SIZE
microsoft/nanoserver   1709                17e7aa2027ea        3 weeks ago         236MB
microsoft/nanoserver   latest              17e7aa2027ea        3 weeks ago         236MB
```

5. From a PowerShell console windows on your development machine build the ContainerApp project and create a Docker image.

```
# change to the ContainerApp directory
cd .\ContainerApp

# optionally remove the published directory
rd .\published\ -Recurse -Force

# build the ContainerApp as a self-contained app
dotnet publish -o published --self-contained -r win10-x64

# build the docker image
docker build . -t containerapp:latest
```

The **docker images** output now looks like this:

```
REPOSITORY             TAG                 IMAGE ID            CREATED              SIZE
containerapp           latest              aa672ae40de4        About a minute ago   301MB
microsoft/nanoserver   1709                17e7aa2027ea        3 weeks ago          236MB
microsoft/nanoserver   latest              17e7aa2027ea        3 weeks ago          236MB
```

To start the container app:

```
docker run containerapp
```

## Excercises for the user
Try building the Docker image on your development machine and pushing it using (**docker push**) to Docker Hub or Azure.
You can then use **docker pull** on any number of devices to copy the images so long as they are running the version of
the OS that matches the version that the image was built for.

## More Docker commands
<table>
 <tr>
  <td>docker ps</td>
  <td>list running containers</td>
 </tr>
 <tr>
  <td>docker ps -a</td>
  <td>list running and exited containers</td>
 </tr>
 <tr>
  <td>docker rm [container]</td>
  <td>remove a docker container</td>
 </tr>
 <tr>
  <td>docker images</td>
  <td>list docker images</td>
 </tr>
 <tr>
  <td>docker rmi [image]</td>
  <td>remote a docker image</td>
 </tr>
 <tr>
  <td>docker container ls</td>
  <td>list docker containers</td>
 </tr>
 <tr>
  <td>docker container prune -f</td>
  <td>remove containers that are not running</td>
 </tr>
</table>

[Complete reference: Use the Docker command line](https://docs.docker.com/engine/reference/commandline/cli/)