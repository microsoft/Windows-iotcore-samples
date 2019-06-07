ARG IMAGE=mcr.microsoft.com/windows/iotuap
ARG IMAGE_TAG=1809
FROM ${IMAGE}:${TAG}

USER ContainerUser

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

ENV IPInterfaceName vEthernet (Ethernet)

CMD ["AudioLoopback.exe"]
