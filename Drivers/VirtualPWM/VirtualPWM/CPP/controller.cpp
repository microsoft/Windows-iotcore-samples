//
// Copyright (C) Microsoft. All rights reserved.
//
// Module Name:
//
//   controller.cpp
//
// Abstract:
//
//  This module contains methods implementation for the PWM controller that
//  manipulates the hardware directly.
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
#include "controller.tmh"

PWM_NONPAGED_SEGMENT_BEGIN; //==============================================

_Use_decl_annotations_
NTSTATUS
PwmControllerGetInfo (
    _In_ const PWM_DEVICE_CONTEXT* /*DeviceContextPtr*/,
    _Out_ PWM_CONTROLLER_INFO* ControllerInfoPtr
    )
{
    ControllerInfoPtr->Size = sizeof(PWM_CONTROLLER_INFO);
    ControllerInfoPtr->PinCount = PWM_PIN_COUNT;

    //
    // TODO:
    // Minimum and maximum period are usually based on the PWM clock source
    // configuration. These info can be either queried from the PEP driver,
    // ACPI, registry, or even hardcoded which is the least portable option.
    //
    ControllerInfoPtr->MaximumPeriod = PWM_MAX_PERIOD;
    ControllerInfoPtr->MinimumPeriod = PWM_MIN_PERIOD;

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmSoftReset (
    PWM_DEVICE_CONTEXT* DeviceContextPtr
    )
{
    //
    // TODO:
    // 1) Perform controller hardware initialization which should bring the
    //    controller into a clean default state.
    // 2) Reset software maintained state for the controller and its child
    //    pins to their default values.
    //

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

    for (ULONG i = 0; i < PWM_PIN_COUNT; ++i) {
        DeviceContextPtr->Pin[i].ActiveDutyCycle = 0;
        DeviceContextPtr->Pin[i].Polarity = PWM_ACTIVE_HIGH;
        DeviceContextPtr->Pin[i].IsStarted = false;
    }

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmSetDesiredPeriod (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    PWM_PERIOD DesiredPeriod
    )
{
    NT_ASSERT(
        (DesiredPeriod >= DeviceContextPtr->ControllerInfo.MinimumPeriod) &&
        (DesiredPeriod <= DeviceContextPtr->ControllerInfo.MaximumPeriod));

    //
    // TODO:
    // Program the controller period to a period nearest to the DesiredPeriod.
    // Set DeviceContextPtr->ActualPeriod to the effective period that the
    // controller is actually capable of producing.
    //

    DeviceContextPtr->DesiredPeriod = DesiredPeriod;
    DeviceContextPtr->ActualPeriod = DesiredPeriod;

    PWM_LOG_INFORMATION(
        "Setting new period. "
        "(DesiredPeriod = %llups(%lluHz), ActualPeriod = %llups(%lluHz))",
        DesiredPeriod,
        PwmPeriodToFrequency(DesiredPeriod),
        DeviceContextPtr->ActualPeriod,
        PwmPeriodToFrequency(DeviceContextPtr->ActualPeriod));

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmSetActiveDutyCycle (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    PWM_PERCENTAGE ActiveDutyCycle
    )
{
    PWM_PIN_STATE* pinPtr = DeviceContextPtr->Pin + PinNumber;

    //
    // TODO:
    // Program the PWM controller to change the duty cycle. The percentage
    // can be computed as ActiveDutyCycle / PWM_PERCENTAGE_MAX.
    // Return STATUS_PENDING if the request will be completed asynchronously
    // in some DPC or a Workitem, otherwise return STATUS_SUCCESS if changing
    // polarity can be done inline.
    //
    // For PWM controllers with a Fifo for the PWM samples, this method will
    // be initiating the single sample transfer to the FIFO. Completing the
    // SetActiveDutyCycle request means that the sample made its way to the
    // Fifo at least, and not necessary made an immediate effect to the PWM
    // signal.
    //

    pinPtr->ActiveDutyCycle = ActiveDutyCycle;

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmSetPolarity (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    PWM_POLARITY Polarity
    )
{
    PWM_PIN_STATE* pinPtr = DeviceContextPtr->Pin + PinNumber;

    //
    // TODO:
    // Program the controller to change the PWM polarity. If the controller
    // supports a single type of polarity (commonly Active High). Then simulating
    // an Active Low is possible by using the complement of the duty cycle.
    //

    pinPtr->Polarity = Polarity;

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmStart (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber
    )
{
    PWM_PIN_STATE* pinPtr = DeviceContextPtr->Pin + PinNumber;
    NT_ASSERT(!pinPtr->IsStarted);

    //
    // TODO:
    // Program the controller to start/enable the PWM on the pin.
    //

    pinPtr->IsStarted = true;

    return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
PwmStop (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber
    )
{
    PWM_PIN_STATE* pinPtr = DeviceContextPtr->Pin + PinNumber;
    NT_ASSERT(pinPtr->IsStarted);

    //
    // TODO:
    // Program the controller to stop/disable the PWM on the pin. This call
    // should not reset the PWM controller to its default state.
    //

    pinPtr->IsStarted = false;

    return STATUS_SUCCESS;
}

PWM_NONPAGED_SEGMENT_END; //=================================================