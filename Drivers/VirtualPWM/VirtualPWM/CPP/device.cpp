//
// Copyright (C) Microsoft. All rights reserved.
//
// Module Name:
//
//   device.cpp
//
// Abstract:
//
//  This module contains methods implementation for the device initialization
//  and PNP callbacks.
//
// Environment:
//
//  Kernel mode only
//

#include "precomp.h"
#pragma hdrstop

#include "virtualpwm.hpp"
#include "utility.hpp"
#include "trace.h"
#include "device.tmh"

PWM_PAGED_SEGMENT_BEGIN; //==================================================

_Use_decl_annotations_
NTSTATUS
PwmEvtDevicePrepareHardware (
    WDFDEVICE /*WdfDevice*/,
    WDFCMRESLIST /*ResourcesRaw*/,
    WDFCMRESLIST /*ResourcesTranslated*/
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    PWM_LOG_TRACE("()");

    //
    // TODO:
    // Look for expected memory, interrupt and DMA resources.
    //
    // NOTE:
    // Don't initialize the controller or do any register read/write yet, that
    // will be done later in the D0 callback.
    //

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmEvtDeviceReleaseHardware (
    WDFDEVICE /*WdfDevice*/,
    WDFCMRESLIST /*ResourcesTranslated*/
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    PWM_LOG_TRACE("()");

    //
    // TODO:
    // Free device specific resources that are not managed by the framework.
    //

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmResetControllerDefaults (
    PWM_DEVICE_CONTEXT* DeviceContextPtr
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    NTSTATUS status =
        PwmSetDesiredPeriod(
            DeviceContextPtr,
            DeviceContextPtr->DefaultDesiredPeriod);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmSetDesiredPeriod(...) failed. (status = %!STATUS!)",
            status);
        return status;
    }

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmResetPinDefaults (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    PWM_PIN_STATE* pinPtr = DeviceContextPtr->Pin + PinNumber;
    NTSTATUS status;

    if (pinPtr->IsStarted) {
        status = PwmStop(DeviceContextPtr, PinNumber);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "PwmStop(...) failed. (status = %!STATUS!)",
                status);
            return status;
        }
    }

    pinPtr->ActiveDutyCycle = 0;

    status = PwmSetPolarity(DeviceContextPtr, PinNumber, PWM_ACTIVE_HIGH);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmSetPolarity(...) failed. (status = %!STATUS!)",
            status);
        return status;
    }

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmEvtDeviceD0Entry (
    WDFDEVICE WdfDevice,
    WDF_POWER_DEVICE_STATE /* PreviousState */
    )
{
    //
    // D0Entry callback is special. It will be called at PASSIVE_LEVEL IRQL
    // but shouldn't be pageable since it is in the wakeup path and the
    // page file may not be ready by that time.
    //
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    PWM_LOG_TRACE("()");

    PWM_DEVICE_CONTEXT* deviceContextPtr = PwmGetDeviceContext(WdfDevice);
    NTSTATUS status = PwmSoftReset(deviceContextPtr);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmSoftReset(...) failed. (status = %!STATUS!)",
            status);
        return status;
    }

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmCreateDeviceInterface (
    PWM_DEVICE_CONTEXT* DeviceContextPtr
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);
    
    NTSTATUS status =
        WdfDeviceCreateDeviceInterface(
                DeviceContextPtr->WdfDevice,
                &GUID_DEVINTERFACE_PWM_CONTROLLER,
                nullptr);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfDeviceCreateDeviceInterface(...) failed. (status = %!STATUS!)",
            status);

        return status;
    }

    //
    // Retrieve device interface symbolic link string
    //
    {
        WDF_OBJECT_ATTRIBUTES attributes;
        WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
        attributes.ParentObject = DeviceContextPtr->WdfDevice;
        status =
            WdfStringCreate(
                nullptr,
                &attributes,
                &DeviceContextPtr->DeviceInterfaceSymlinkName);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "WdfStringCreate(...) failed. (status = %!STATUS!)",
                status);

            return status;
        }

        status =
            WdfDeviceRetrieveDeviceInterfaceString(
                DeviceContextPtr->WdfDevice,
                &GUID_DEVINTERFACE_PWM_CONTROLLER,
                nullptr,
                DeviceContextPtr->DeviceInterfaceSymlinkName);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "WdfDeviceRetrieveDeviceInterfaceString(...) failed. "
                "(status = %!STATUS!, GUID_DEVINTERFACE_PWM_CONTROLLER = %!GUID!)",
                status,
                &GUID_DEVINTERFACE_PWM_CONTROLLER);

            return status;
        }

        WdfStringGetUnicodeString(
            DeviceContextPtr->DeviceInterfaceSymlinkName,
            &DeviceContextPtr->DeviceInterfaceSymlinkNameWsz);
    }

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmEvtDeviceAdd (
    WDFDRIVER /*WdfDriver*/,
    WDFDEVICE_INIT* DeviceInitPtr
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    //
    // Set PNP and Power callbacks
    //
    {
        WDF_PNPPOWER_EVENT_CALLBACKS wdfPnpPowerEventCallbacks;
        WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&wdfPnpPowerEventCallbacks);
        wdfPnpPowerEventCallbacks.EvtDevicePrepareHardware =
            PwmEvtDevicePrepareHardware;

        wdfPnpPowerEventCallbacks.EvtDeviceReleaseHardware =
            PwmEvtDeviceReleaseHardware;
        wdfPnpPowerEventCallbacks.EvtDeviceD0Entry =
            PwmEvtDeviceD0Entry;

        WdfDeviceInitSetPnpPowerEventCallbacks(
            DeviceInitPtr,
            &wdfPnpPowerEventCallbacks);

    }

    //
    // Configure file create/close callbacks
    //
    {
        WDF_FILEOBJECT_CONFIG wdfFileObjectConfig;
        WDF_FILEOBJECT_CONFIG_INIT(
            &wdfFileObjectConfig,
            PwmEvtDeviceFileCreate,
            PwmEvtFileClose,
            WDF_NO_EVENT_CALLBACK); // not interested in Cleanup

        WdfDeviceInitSetFileObjectConfig(
            DeviceInitPtr,
            &wdfFileObjectConfig,
            WDF_NO_OBJECT_ATTRIBUTES);
    }

    NTSTATUS status;

    //
    // Create and initialize the WDF device
    //
    WDFDEVICE wdfDevice;
    PWM_DEVICE_CONTEXT* deviceContextPtr;
    {
        WDF_OBJECT_ATTRIBUTES attributes;
        WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(
            &attributes,
            PWM_DEVICE_CONTEXT);

        status = WdfDeviceCreate(&DeviceInitPtr, &attributes, &wdfDevice);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "WdfDeviceCreate(...) failed. (status = %!STATUS!)",
                status);

            return status;
        }

        WDF_OBJECT_ATTRIBUTES wdfObjectAttributes;
        WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(
            &wdfObjectAttributes,
            PWM_DEVICE_CONTEXT);

        void* contextPtr;
        status =
            WdfObjectAllocateContext(
                wdfDevice,
                &wdfObjectAttributes,
                &contextPtr);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "WdfObjectAllocateContext(...) failed. (status = %!STATUS!)",
                status);

            return status;
        }

        deviceContextPtr = static_cast<PWM_DEVICE_CONTEXT*>(contextPtr);
        deviceContextPtr->WdfDevice = wdfDevice;
    }

    //
    // Create controller and pin locks
    //
    {
        WDF_OBJECT_ATTRIBUTES attributes;
        WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
        attributes.ParentObject = wdfDevice;
        status =
            WdfWaitLockCreate(&attributes, &deviceContextPtr->ControllerLock);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "WdfWaitLockCreate(...) failed. (status = %!STATUS!)",
                status);

            return status;
        }

        for (ULONG i = 0; i < PWM_PIN_COUNT; ++i) {
            status = WdfWaitLockCreate(&attributes, &deviceContextPtr->Pin[i].Lock);
            if (!NT_SUCCESS(status)) {
                PWM_LOG_ERROR(
                    "WdfWaitLockCreate(...) failed. (status = %!STATUS!)",
                    status);

                return status;
            }
        }
    }

    //
    // Set PNP capabilities.
    //
    {
        WDF_DEVICE_PNP_CAPABILITIES pnpCaps;
        WDF_DEVICE_PNP_CAPABILITIES_INIT(&pnpCaps);

        pnpCaps.Removable = WdfFalse;
        pnpCaps.SurpriseRemovalOK = WdfFalse;

        WdfDeviceSetPnpCapabilities(wdfDevice, &pnpCaps);
    }

    //
    // Make the device disable-able
    //
    {
        WDF_DEVICE_STATE wdfDeviceState;
        WDF_DEVICE_STATE_INIT(&wdfDeviceState);

        wdfDeviceState.NotDisableable = WdfFalse;
        WdfDeviceSetDeviceState(wdfDevice, &wdfDeviceState);
    }

    //
    // Set controller info and defaults based on its capabilities.
    //
    {
        PWM_CONTROLLER_INFO* infoPtr = &deviceContextPtr->ControllerInfo;
        status = PwmControllerGetInfo(deviceContextPtr, infoPtr);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "PwmControllerGetInfo(...) failed. (status = %!STATUS!)",
                status);
        }

        deviceContextPtr->DefaultDesiredPeriod = infoPtr->MinimumPeriod;

        NT_ASSERT(
            (infoPtr->MinimumPeriod > 0) &&
            (infoPtr->MinimumPeriod <= infoPtr->MaximumPeriod) &&
            (deviceContextPtr->DefaultDesiredPeriod > 0));
        PWM_LOG_INFORMATION(
            "Controller Info: PinCount = %lu, MinimumPeriod = %llups(%lluHz), "
            "MaximumPeriod = %llups(%lluHz), DefaultPeriod = %llups(%lluHz)",
            infoPtr->PinCount,
            infoPtr->MinimumPeriod,
            PwmPeriodToFrequency(infoPtr->MinimumPeriod),
            infoPtr->MaximumPeriod,
            PwmPeriodToFrequency(infoPtr->MaximumPeriod),
            deviceContextPtr->DefaultDesiredPeriod,
            PwmPeriodToFrequency(deviceContextPtr->DefaultDesiredPeriod));
    }

    //
    // Create default sequential dispatch queue.
    //
    {
        WDF_IO_QUEUE_CONFIG wdfQueueConfig;
        WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(
            &wdfQueueConfig,
            WdfIoQueueDispatchSequential);
        wdfQueueConfig.EvtIoDeviceControl = PwmEvtIoDeviceControl;

        WDF_OBJECT_ATTRIBUTES wdfQueueAttributes;
        WDF_OBJECT_ATTRIBUTES_INIT(&wdfQueueAttributes);
        wdfQueueAttributes.ExecutionLevel = WdfExecutionLevelPassive;

        WDFQUEUE wdfQueue;
        status = WdfIoQueueCreate(
                wdfDevice,
                &wdfQueueConfig,
                &wdfQueueAttributes,
                &wdfQueue);

        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "WdfIoQueueCreate(..) failed. (status=%!STATUS!)",
                status);

            return status;
        }
    }

    //
    // Publish controller device interface.
    //
    status = PwmCreateDeviceInterface(deviceContextPtr);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmCreateDeviceInterface(...) failed. (status = %!STATUS!)",
            status);
        return status;
    }

    PWM_LOG_INFORMATION(
        "Published device interface %wZ",
        &deviceContextPtr->DeviceInterfaceSymlinkNameWsz);

    return STATUS_SUCCESS;
}

PWM_PAGED_SEGMENT_END; //===================================================