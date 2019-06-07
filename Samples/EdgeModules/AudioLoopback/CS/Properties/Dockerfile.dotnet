ARG IMAGE=mcr.microsoft.com/dotnet/core/runtime
ARG IMAGE_TAG=2.2-iotuap-1809
FROM ${IMAGE}:${TAG}

USER ContainerUser

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

ENV IPInterfaceName vEthernet (Ethernet)

CMD ["dotnet.exe AudioLoopback.dll"]
