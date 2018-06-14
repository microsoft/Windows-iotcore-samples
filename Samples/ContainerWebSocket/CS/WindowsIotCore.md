# Using WebSockets to communicate with Docker containers on Windows IoT Core

## Before You Begin

This sample requires a 64-bit OS running on an Intel architecture board like MinnowBoard Max or Minnowboard Turbot.

The following Visual Studio 2017 project types and templates must be installed
* Universal Windows Platform development tools
* .Net Core 2.0 development tools
* [**Windows IoT Core Project Templates** extension.](https://marketplace.visualstudio.com/items?itemName=MicrosoftIoT.WindowsIoTCoreProjectTemplatesforVS15)

## Install Docker on Windows IoT Core
Copy the setup files from the ./Scripts directory to c:\scripts on the Windows IoT Core device.
For if the device's IP address is 192.168.2.3:

```
net use X: \\192.168.2.3\c$ /user:192.168.2.3\Administrator
md X:\scripts
copy .\scripts\DeployDocker.ps1 X:\scripts
copy .\scripts\ConfigureFirewall.ps1 X:\scripts
```

Connect to the device from your development machine with PowerShell.  [See Using PowerShell for Windows IoT](https://docs.microsoft.com/en-us/windows/iot-core/connect-your-device/powershell). In the remote PowerShell window run the following commands:

```
cd c:\scripts
.\DeployDocker.ps1
.\ConfigureFirewall.ps1
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

## Build and run the WebSocket sample server
1. Open websocket.sln in Visual Studio 2017
2. Set BackgroundServer as the startup project by right clicking on the project in the Solution Explorer and choosing **Set as Startup Project**
3. On the Debug toolbar choose **Debug**, **x64**, and **Remote Machine**. When prompted enter the IP Address of your Windows IoT Core device.  The **Debug** authentication mode in the project properties should be **Universal** (which is the default).
4. Select Debug>Start Debugging (F5)


## Build and run the WebSocket sample client
From a PowerShell console windows on your development machine build the ContainerApp project. Then copy the published files to the Windows IoT Core device:

```
# change to the ContainerApp directory
cd .\ContainerApp

# optionally remove the published directory
rd .\published\ -Recurse -Force

# build the ContainerApp as a self-contained app
dotnet publish -o published --self-contained -r win10-x64

# copy the published directory
md z:\ContainerApp\published
copy .\published z:\ContainerApp\published -Recurse
copy .\Dockerfile z:\ContainerApp
```
Using a remote PowerShell console to your Windows IoT Core device:

```
cd C:\ContainerApp\
docker build . -t containerapp:latest
docker images
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

Docker interactive consoles don't work with remote PowerShell.  If you want to want to use Ctrl-C and interact with the console interactively then connect with SSH and use the interactive (-it) option.

```
docker run -it containerapp
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