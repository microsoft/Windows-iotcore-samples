# Client connection to RPC

The client is an usual UWP app. The file [RpcClient.cpp](../cpp/RpcClient.cpp) has function `Initialize`, which opens the RPC connection passing a pointer to a pointer to a context for the service to use. The service will allocate whatever it wants as a context and write the pointer. All other functions simply call RPC interfaces passing the context and any arguments.

However, the RPC functions can block and can't be called from the UI thread. Calling a blocking operation on the UI thread is forbidden in UWP to keep the UI responsive. We can use asynchronous tasks to call the RPC endpoints and update the UI whenever they return. This behavior is implemented in [RpcAsyncWrapper.cpp](../cpp/RpcAsyncWrapper.cpp), which is a wrapper returning `Concurrency::task` instances.

From the application, we can now use the asynchronous functions and give them completion callbacks. For example, in `MainPage.xaml.cpp`, the code for the `Get Status` button calls our wrapper and updates the UI in the callback:

```cpp
std::wstring ServiceStatusString(RpcAsyncWrapper::ServiceStatus status)
{
    switch (status)
    {
    case RpcAsyncWrapper::SERVICE_STOPPED:
        return L"Service is stopped";
    case RpcAsyncWrapper::SERVICE_START_PENDING:
        return L"Service's start is pending";
    case RpcAsyncWrapper::SERVICE_STOP_PENDING:
        return L"Service's stop is pending";
    case RpcAsyncWrapper::SERVICE_RUNNING:
        return L"Service is running";
    case RpcAsyncWrapper::SERVICE_CONTINUE_PENDING:
        return L"Service's continue is pending";
    case RpcAsyncWrapper::SERVICE_PAUSE_PENDING:
        return L"Service's pause is pending";
    case RpcAsyncWrapper::SERVICE_PAUSED:
        return L"Service is paused";
    default:
        return L"Error getting service status: " + std::to_wstring(status);
    }
}

...

void MainPage::button_Click_GetStatus(Object^ sender, RoutedEventArgs^ e)
{
    rpc.GetServiceStatus(ServiceNameTextBox->Text->Data()).then([this](RpcAsyncWrapper::ServiceStatus status) {
        NotifyUser(ref new Platform::String((L"Status: " + ServiceStatusString(status)).c_str()));
    });
}
```
