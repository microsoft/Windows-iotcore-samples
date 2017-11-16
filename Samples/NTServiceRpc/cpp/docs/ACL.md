# Using ACLs to limit access to the NT service

In the RPC server of the NT service, we want to allow access only from known applications. We can
limit the access with Access Control Lists (ACL).

The ACL can contain, for example, rules to require the existence of a capability (e.g. only
applications with the system management capability, or with a custom capability) or a specific
Package Family Name (PFN). For an example using a custom capability, see [this Windows universal sample](https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/CustomCapability)
and [documentation to create and reserve a custom capability](https://docs.microsoft.com/en-us/windows-hardware/drivers/devapps/creating-a-custom-capability-to-pair-driver-with-hsa).
In this sample, only a specific PFN will be able to connect to the service. In [RpcServer.cpp](../../cpp/Service/Server/RpcServer.cpp),
we have a function (`BuildAcl`) to create the ACL:

* First, it gets a security identifier for everyone running outside an AppContainer. Non-sandboxed
apps will have access to the service.
* Then, it gets a security identifier for our app name.
* An ACL is built from these two entries.

```cpp
const PWSTR AllowedPackageFamilyName = L"Microsoft.SDKSamples.NTServiceRpc.CPP_8wekyb3d8bbwe";

PACL BuildAcl()
{
    SID_IDENTIFIER_AUTHORITY SIDAuthWorld = SECURITY_WORLD_SID_AUTHORITY;
    PSID everyoneSid = nullptr;
    EXPLICIT_ACCESS ea[2] = {};

    // Get the SID that represents 'everyone' (this doesn't include AppContainers)
    if (!AllocateAndInitializeSid(
        &SIDAuthWorld, 1,
        SECURITY_WORLD_RID,
        0, 0, 0, 0, 0, 0, 0,
        &everyoneSid))
    {
        ThrowLastError("AllocateAndInitializeSid");
    }
    // Everyone GENERIC_ALL access
    ea[0].grfAccessMode = SET_ACCESS;
    ea[0].grfAccessPermissions = GENERIC_ALL;
    ea[0].grfInheritance = NO_INHERITANCE;
    ea[0].Trustee.TrusteeForm = TRUSTEE_IS_SID;
    ea[0].Trustee.TrusteeType = TRUSTEE_IS_WELL_KNOWN_GROUP;
    ea[0].Trustee.ptstrName = static_cast<LPWSTR>(everyoneSid);

    // When creating the RPC endpoint we want it to allow connections from the UWA with a
    // specific package family name. By default, RPC endpoints don't allow UWAs (AppContainer processes)
    // to connect to them, so we need to set the security on the endpoint to allow access to our specific
    // UWA.
    //
    // To do this we'll perform the following steps:
    // 1) Create a SID for the allowed package family name
    // 2) Create a security descriptor using that SID
    // 3) Create the RPC endpoint using that security descriptor
    PSID pfnSid = nullptr;
    HRESULT hResult = DeriveAppContainerSidFromAppContainerName(AllowedPackageFamilyName, &pfnSid);
    FreeSid(everyoneSid);
    if (hResult != S_OK)
    {
        ThrowLastError("DeriveAppContainerSidFromAppContainerName", hResult);
    }

    ea[1].grfAccessMode = SET_ACCESS;
    ea[1].grfAccessPermissions = GENERIC_ALL;
    ea[1].grfInheritance = NO_INHERITANCE;
    ea[1].Trustee.TrusteeForm = TRUSTEE_IS_SID;
    ea[1].Trustee.TrusteeType = TRUSTEE_IS_UNKNOWN;
    ea[1].Trustee.ptstrName = static_cast<LPWSTR>(pfnSid);
    PACL acl = nullptr;
    hResult = SetEntriesInAcl(ARRAYSIZE(ea), ea, nullptr, &acl);
    FreeSid(pfnSid);
    if (hResult != ERROR_SUCCESS)
    {
        ThrowLastError("DeriveAppContainerSidFromAppContainerName", hResult);
    }
    return acl;
}
```

The `RpcServerStart` function will start a RPC server using the ACL returned by `BuildAcl`.
