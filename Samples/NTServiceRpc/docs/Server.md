# RPC server implementation

The RPC server runs outside the app container and has access to the full Windows APIs.

## Windows API calls

The Windows API calls to control services are done in file [ServiceControl.cpp](../cpp/ServiceControl.cpp). The `ServiceControl` class exposes the functionality through `GetServiceStatus`,
`RunService` and `StopService`.

## Handling RPC calls

The methods in [RpcServer.cpp](../Service/Server/RpcServer.cpp) call the functions in ServiceControl.cpp. The `RemoteOpen` method, called by the client to start a connection, is called with a pointer to a pointer where the service can save a context. Our service allocates an instance of `ServiceControl` and saves its pointer. The `RemoteClose` method cleans it up (`delete`s it).

All other calls cast the context and call the corresponding method in the `ServiceControl` instance:

```cpp
DWORD GetServiceStatus(
    _In_ PCONTEXT_HANDLE_TYPE phContext,
    _In_ const wchar_t *serviceName)
{
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(phContext);
    return serviceControlContext->serviceControl->GetServiceStatus(serviceName);
}
```
