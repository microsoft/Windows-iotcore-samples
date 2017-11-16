# Client connection to RPC

The client is an usual UWP app. The file [RpcClient.cpp](../RpcClient.cpp) has function `Initialize`, which opens the RPC connection passing a pointer to a pointer to a context for the service to use. The service will allocate whatever it wants as a context and write the pointer. All other functions simply call RPC interfaces passing the context and any arguments.

However, the RPC functions can block and can't be called from the UI thread. Calling a blocking operation on the UI thread is forbidden in UWP to keep the UI responsive. We can use asynchronous tasks to call the RPC endpoints and update the UI whenever they return. This behavior is implemented in [RpcAsyncWrapper.cpp](../RpcAsyncWrapper.cpp), which is a wrapper returning `Concurrency::task` instances.

From the application, we can now use the asynchronous functions and give them completion callbacks. For example, in `MainPage.xaml.cpp`, the code for the `Start` button calls our wrapper, catches exceptions and shows them in the UI, or updates the UI with the result:

```cpp
void MainPage::Start_Click(Object^, RoutedEventArgs^)
{
    rpc.RunService(ServiceNameTextBox->Text->Data()).then([this](Concurrency::task<void> t)
    {
        CatchRpcException(t, static_cast<function<void()>>([this]() {
            NotifyUser("Service started");
        }));
    });
}
```
