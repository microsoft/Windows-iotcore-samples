# RPC server implementation

The RPC server runs outside the app container and has access to the full Windows APIs.

## Windows API calls

The Windows API calls to control services are done in file [ServiceControl.cpp](../ServiceControl.cpp). The `ServiceControl` class exposes the functionality through `GetServiceStatus`,
`RunService` and `StopService`.

## Handling RPC calls

The methods in [RpcServer.cpp](../Service/Server/RpcServer.cpp) call the functions in ServiceControl.cpp. The `RemoteOpen` method, called by the client to start a connection, is called with a pointer to a pointer where the service can save a context. Our service allocates an instance of `ServiceControl` and saves its pointer. The `RemoteClose` method cleans it up (`delete`s it).

All other calls cast the context and call the corresponding method in the `ServiceControl` instance, catching exceptions and returning them as error codes to the RPC client. For example, method `RunService`:

```cpp
DWORD RunService(
    PCONTEXT_HANDLE_TYPE phContext,
    const wchar_t *serviceName)
{
    SERVICE_CONTROL_CONTEXT* serviceControlContext = static_cast<SERVICE_CONTROL_CONTEXT *>(phContext);
    try
    {
        serviceControlContext->serviceControl->RunService(serviceName);
        return 0;
    }
    catch (WindowsCodeError& e)
    {
        return e.code;
    }
}
```
