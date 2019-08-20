---
page_type: sample
urlFragment: NTService-RPC
languages:
  - csharp
products:
  - windows
description: This sample demonstrates the use of a NT service with an UWP app and Windows 10 IoT Core.
---

# NT Service example

This sample demonstrates the use of a NT service with an UWP app. Communication between the app and
the service is done through RPC.

## Goal

This sample shows how UWP can communicate with a NT service, allowing it to perform privileged
actions. In this sample, the UWP app will check the status of services and start/stop them.

## Security

The NT service must be not open to any application, since an untrusted application would have
control of system services. We can limit the access with Access Control Lists (ACL) on the RPC
server.

The ACL can contain, for example, rules to require the existence of a capability (e.g. only
applications with the system management capability, or with a custom capability) or a specific
Package Family Name (PFN). In this example, only a specific PFN will be able to connect to the
service.

## Platforms

This sample can run on ARM and x86, on both Windows for IoT and desktop.

## Projects

This solution has three projects:

* **RpcInterface**: Has a IDL file ([Interface Definition Language](https://msdn.microsoft.com/en-us/library/windows/desktop/aa367091(v=vs.85).aspx))
with the definition of the RPC interface (functions and its arguments).
* **RpcServer**: RPC server. Runs as a NT service and receives RPC calls to return the status, start
or stop other NT services.
* **NTServiceRpc**: Sample UWP app consuming the NT service.


## Further information

* [Add a service component to Windows Universal OEM Packages](https://docs.microsoft.com/en-us/windows-hardware/manufacture/iot/create-packages#add-a-service-component)
- To create an OEM package that includes a service. For a sample of such usage, see [this sample on
iot-adk-addonkit](https://github.com/ms-iot/iot-adk-addonkit/blob/26738284601eceeebc9989f884a411ae452d2f3a/Source-arm/Packages/AzureDM.Services/AzureDM.Services.wm.xml).
