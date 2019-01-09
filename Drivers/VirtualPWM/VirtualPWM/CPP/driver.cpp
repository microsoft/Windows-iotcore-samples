//
// Copyright (C) Microsoft. All rights reserved.
//
// Module Name:
//
//   driver.cpp
//
// Abstract:
//
//   This module contains methods implementation for the driver initialization.
//
// Environment:
//
//  Kernel mode only
//

#include "precomp.h"
#pragma hdrstop

#include "virtualpwm.hpp"

#include "trace.h"
#include "driver.tmh"

PWM_PAGED_SEGMENT_BEGIN; //==================================================

VOID
PwmEvtDriverUnload (
    WDFDRIVER WdfDriver
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    DRIVER_OBJECT* driverObjectPtr = WdfDriverWdmGetDriverObject(WdfDriver);
    WPP_CLEANUP(driverObjectPtr);
}

PWM_PAGED_SEGMENT_END; //===================================================
PWM_INIT_SEGMENT_BEGIN; //==================================================

_Use_decl_annotations_
NTSTATUS
DriverEntry (
    DRIVER_OBJECT* DriverObjectPtr,
    UNICODE_STRING* RegistryPathPtr
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    //
    // Initialize logging
    //
    {
        WPP_INIT_TRACING(DriverObjectPtr, RegistryPathPtr);
        RECORDER_CONFIGURE_PARAMS recorderConfigureParams;
        RECORDER_CONFIGURE_PARAMS_INIT(&recorderConfigureParams);
        WppRecorderConfigure(&recorderConfigureParams);
#if DBG
        //
        // Allow trace level messages in debug build only.
        //
        WPP_RECORDER_LEVEL_FILTER(PWM_TRACING_DEFAULT) = TRUE;
#endif // DBG
    }

    WDF_DRIVER_CONFIG wdfDriverConfig;
    WDF_DRIVER_CONFIG_INIT(&wdfDriverConfig, PwmEvtDeviceAdd);
    wdfDriverConfig.DriverPoolTag = PWM_POOL_TAG;
    wdfDriverConfig.EvtDriverUnload = PwmEvtDriverUnload;

    WDFDRIVER wdfDriver;
    NTSTATUS status = WdfDriverCreate(
            DriverObjectPtr,
            RegistryPathPtr,
            WDF_NO_OBJECT_ATTRIBUTES,
            &wdfDriverConfig,
            &wdfDriver);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "Failed to create WDF driver object. (status = %!STATUS!, "
            "DriverObjectPtr = %p, RegistryPathPtr = %p)",
            status,
            DriverObjectPtr,
            RegistryPathPtr);

        return status;
    }

    return STATUS_SUCCESS;
}

PWM_INIT_SEGMENT_END; //====================================================