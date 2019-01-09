//
// Copyright (C) Microsoft.  All rights reserved.
//
// Module Name:
//
//   virtualpwm.hpp
//
// Abstract:
//
//   This header contains the driver callbacks and types declarations.
//

#ifndef _VIRTUALPWM_HPP_
#define _VIRTUALPWM_HPP_

//
// Macros to be used for proper PAGED/NON-PAGED code placement
//

#define PWM_NONPAGED_SEGMENT_BEGIN \
    __pragma(code_seg(push)) \
    //__pragma(code_seg(.text))

#define PWM_NONPAGED_SEGMENT_END \
    __pragma(code_seg(pop))

#define PWM_PAGED_SEGMENT_BEGIN \
    __pragma(code_seg(push)) \
    __pragma(code_seg("PAGE"))

#define PWM_PAGED_SEGMENT_END \
    __pragma(code_seg(pop))

#define PWM_INIT_SEGMENT_BEGIN \
    __pragma(code_seg(push)) \
    __pragma(code_seg("INIT"))

#define PWM_INIT_SEGMENT_END \
    __pragma(code_seg(pop))

#define PWM_ASSERT_MAX_IRQL(Irql) NT_ASSERT(KeGetCurrentIrql() <= (Irql))

enum : ULONG { PWM_POOL_TAG = 'MWPV' };

enum : ULONG {
    //
    // The PWM controller has 4 channels.
    //
    PWM_PIN_COUNT = 4,
};

enum : ULONGLONG {
    //
    // Arbitrary period values that correspond to 4Hz and 16KHz frequencies.
    //
    PWM_MAX_PERIOD = 250000000000llu,
    PWM_MIN_PERIOD = 62500000llu,
};

enum : ULONGLONG {
    //
    // 1 second has 10^12 picoseconds.
    //
    PICOSECONDS_IN_1_SECOND = 1000000000000llu
};

struct PWM_PIN_STATE {
    PWM_POLARITY Polarity;
    PWM_PERCENTAGE ActiveDutyCycle;
    bool IsStarted;
    bool IsOpenForWrite;
    //
    // A lock to protect IsOpenForWrite during file create/close callbacks.
    //
    WDFWAITLOCK Lock;
}; // struct PWM_PIN_STATE

struct PWM_DEVICE_CONTEXT {
    WDFDEVICE WdfDevice;
    WDFSTRING DeviceInterfaceSymlinkName;
    UNICODE_STRING DeviceInterfaceSymlinkNameWsz;

    PWM_PERIOD DefaultDesiredPeriod;
    bool IsControllerOpenForWrite;

    //
    // A lock to protect the controller IsOpenForWrite during file create/close
    // callbacks.
    //
    WDFWAITLOCK ControllerLock;
    PWM_PERIOD DesiredPeriod;
    PWM_PERIOD ActualPeriod;
    PWM_PIN_STATE Pin[PWM_PIN_COUNT];

    PWM_CONTROLLER_INFO ControllerInfo;

}; // struct PWM_DEVICE_CONTEXT

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(
    PWM_DEVICE_CONTEXT,
    PwmGetDeviceContext);

struct PWM_FILE_OBJECT_CONTEXT {
    bool IsOpenForWrite;
    bool IsPinInterface;
    ULONG PinNumber;
}; // struct PWM_FILE_OBJECT_CONTEXT

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(
    PWM_FILE_OBJECT_CONTEXT,
    PwmGetFileObjectContext);

//
// NONPAGED
//

EVT_WDF_DEVICE_D0_ENTRY PwmEvtDeviceD0Entry;
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL PwmEvtIoDeviceControl;

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlControllerGetInfo(
    _In_ const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlControllerGetActualPeriod (
    _In_ const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlControllerSetDesiredPeriod (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlPinGetActiveDutyCycle (
    _In_ const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlPinSetActiveDutyCycle (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlPinGetPolarity (
    _In_ const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlPinSetPolarity (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlPinStart (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlPinStop (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_max_(DISPATCH_LEVEL)
void
PwmIoctlPinIsStarted (
    _In_ const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ WDFREQUEST WdfRequest
    );

_IRQL_requires_same_
NTSTATUS
PwmSoftReset (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr
    );

_IRQL_requires_same_
NTSTATUS
PwmSetDesiredPeriod (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ PWM_PERIOD DesiredPeriod
    );

_IRQL_requires_same_
NTSTATUS
PwmSetActiveDutyCycle (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ PWM_PERCENTAGE ActiveDutyCycle
    );

_IRQL_requires_same_
NTSTATUS
PwmSetPolarity (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber,
    _In_ PWM_POLARITY Polarity
    );

_IRQL_requires_same_
NTSTATUS
PwmStart (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber
    );

_IRQL_requires_same_
NTSTATUS
PwmStop (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber
    );

//
// PAGED
//

_IRQL_requires_same_
_IRQL_requires_max_(PASSIVE_LEVEL)
NTSTATUS
PwmCreateDeviceInterface (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr
    );

_IRQL_requires_same_
_IRQL_requires_max_(PASSIVE_LEVEL)
NTSTATUS
PwmResetControllerDefaults (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr
    );

_IRQL_requires_same_
_IRQL_requires_max_(PASSIVE_LEVEL)
NTSTATUS
PwmResetPinDefaults (
    _In_ PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _In_ ULONG PinNumber
    );

_IRQL_requires_same_
_IRQL_requires_max_(PASSIVE_LEVEL)
NTSTATUS
PwmControllerGetInfo (
    _In_ const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    _Out_ PWM_CONTROLLER_INFO* ControllerInfoPtr
    );

EVT_WDF_DEVICE_PREPARE_HARDWARE PwmEvtDevicePrepareHardware;
EVT_WDF_DEVICE_RELEASE_HARDWARE PwmEvtDeviceReleaseHardware;
EVT_WDF_DRIVER_DEVICE_ADD PwmEvtDeviceAdd;
EVT_WDF_DRIVER_UNLOAD PwmDriverUnload;
EVT_WDF_DEVICE_FILE_CREATE PwmEvtDeviceFileCreate;
EVT_WDF_FILE_CLOSE PwmEvtFileClose;

extern "C" DRIVER_INITIALIZE DriverEntry;

#endif // _VIRTUALPWM_HPP_