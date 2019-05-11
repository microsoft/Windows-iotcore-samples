#
# At the current time the dotnet core team isn't releasing pre-built IoT Core images with dotnet already installed
# you can create your own by copying their nanoserver dockerfile from here:
# https://github.com/dotnet/dotnet-docker/<dotnet core version>/runtime/nanoserver
# and changing the windows base image from nanoserver to iotcore
#
#ARG IMAGE=mcr.microsoft.com/dotnet/core/runtime
#ARG IMAGE_TAG=2.2-iotcore-1809
FROM ${IMAGE}:${TAG}

USER ContainerUser

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

ENV IPInterfaceName vEthernet (Ethernet)

CMD ["dotnet.exe WinMLCustomVisionFruit.dll"]
