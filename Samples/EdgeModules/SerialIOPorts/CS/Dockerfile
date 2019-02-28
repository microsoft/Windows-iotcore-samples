FROM mcr.microsoft.com/windows/nanoserver:1809

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

CMD [ "SerialIOPorts.exe", "-rs", "-dCOM3" ]