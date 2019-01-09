//
// Copyright (C) Microsoft. All rights reserved.
//
// Module Name:
//
//   ioctl.cpp
//
// Abstract:
//
//  This module contains PWM IOCTLs implementation.
//
// Environment:
//
//  Kernel mode only
//

#include "precomp.h"
#pragma hdrstop

#include "virtualpwm.hpp"

#include "trace.h"
#include "ioctl.tmh"

PWM_NONPAGED_SEGMENT_BEGIN; //==============================================

_Use_decl_annotations_
void
PwmEvtIoDeviceControl (
    WDFQUEUE WdfQueue,
    WDFREQUEST WdfRequest,
    size_t /*OutputBufferLength*/,
    size_t /*InputBufferLength*/,
    ULONG IoControlCode
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);

    WDFDEVICE wdfDevice = WdfIoQueueGetDevice(WdfQueue);
    WDFFILEOBJECT wdfFileObject = WdfRequestGetFileObject(WdfRequest);
    PWM_FILE_OBJECT_CONTEXT* fileObjectContextPtr =
        PwmGetFileObjectContext(wdfFileObject);
    PWM_DEVICE_CONTEXT* deviceContextPtr = PwmGetDeviceContext(wdfDevice);

    if (fileObjectContextPtr->IsPinInterface) {
        //
        // Is Pin Interface
        //
        const ULONG pinNumber = fileObjectContextPtr->PinNumber;
        switch (IoControlCode) {
        case IOCTL_PWM_CONTROLLER_GET_INFO:
        case IOCTL_PWM_CONTROLLER_GET_ACTUAL_PERIOD:
        case IOCTL_PWM_CONTROLLER_SET_DESIRED_PERIOD:
            PWM_LOG_INFORMATION(
                "Controller IOCTL directed to pin%lu. (IoControlCode = 0x%x)",
                pinNumber,
                IoControlCode);

            WdfRequestComplete(WdfRequest, STATUS_INVALID_DEVICE_REQUEST);
            break;
        case IOCTL_PWM_PIN_GET_POLARITY:
            PwmIoctlPinGetPolarity(deviceContextPtr, pinNumber, WdfRequest);
            break;
        case IOCTL_PWM_PIN_SET_POLARITY:
            PwmIoctlPinSetPolarity(deviceContextPtr, pinNumber, WdfRequest);
            break;
        case IOCTL_PWM_PIN_GET_ACTIVE_DUTY_CYCLE_PERCENTAGE:
            PwmIoctlPinGetActiveDutyCycle(deviceContextPtr, pinNumber, WdfRequest);
            break;
        case IOCTL_PWM_PIN_SET_ACTIVE_DUTY_CYCLE_PERCENTAGE:
            PwmIoctlPinSetActiveDutyCycle(deviceContextPtr, pinNumber, WdfRequest);
            break;
        case IOCTL_PWM_PIN_START:
            PwmIoctlPinStart(deviceContextPtr, pinNumber, WdfRequest);
            break;
        case IOCTL_PWM_PIN_STOP:
            PwmIoctlPinStop(deviceContextPtr, pinNumber, WdfRequest);
            break;
        case IOCTL_PWM_PIN_IS_STARTED:
            PwmIoctlPinIsStarted(deviceContextPtr, pinNumber, WdfRequest);
            break;
        default:
            PWM_LOG_INFORMATION(
                "IOCTL not supported. (IoControlCode = 0x%x)",
                IoControlCode);

            WdfRequestComplete(WdfRequest, STATUS_NOT_SUPPORTED);
            return;
        }
    } else {
        //
        // Is Controller Interface
        //
        switch (IoControlCode) {
        case IOCTL_PWM_CONTROLLER_GET_INFO:
            PwmIoctlControllerGetInfo(deviceContextPtr, WdfRequest);
            break;
        case IOCTL_PWM_CONTROLLER_GET_ACTUAL_PERIOD:
            PwmIoctlControllerGetActualPeriod(deviceContextPtr, WdfRequest);
            break;
        case IOCTL_PWM_CONTROLLER_SET_DESIRED_PERIOD:
            PwmIoctlControllerSetDesiredPeriod(deviceContextPtr, WdfRequest);
            break;
        case IOCTL_PWM_PIN_GET_POLARITY:
        case IOCTL_PWM_PIN_SET_POLARITY:
        case IOCTL_PWM_PIN_GET_ACTIVE_DUTY_CYCLE_PERCENTAGE:
        case IOCTL_PWM_PIN_SET_ACTIVE_DUTY_CYCLE_PERCENTAGE:
        case IOCTL_PWM_PIN_START:
        case IOCTL_PWM_PIN_STOP:
        case IOCTL_PWM_PIN_IS_STARTED:
            PWM_LOG_INFORMATION(
                "Pin IOCTL directed to a controller. (IoControlCode = 0x%x)",
                IoControlCode);

            WdfRequestComplete(WdfRequest, STATUS_INVALID_DEVICE_REQUEST);
            break;
        default:
            PWM_LOG_INFORMATION("IOCTL not supported. (IoControlCode = 0x%x)", IoControlCode);
            WdfRequestComplete(WdfRequest, STATUS_NOT_SUPPORTED);
            return;
        }
    }
}

_Use_decl_annotations_
void
PwmIoctlControllerGetInfo(
    const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("()");

    PWM_CONTROLLER_GET_INFO_OUTPUT* outputBufferPtr;
    NTSTATUS status = WdfRequestRetrieveOutputBuffer(
            WdfRequest,
            sizeof(*outputBufferPtr),
            reinterpret_cast<PVOID*>(&outputBufferPtr),
            nullptr);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveOutputBuffer(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    RtlCopyMemory(
        outputBufferPtr,
        &DeviceContextPtr->ControllerInfo,
        sizeof(*outputBufferPtr));

    WdfRequestCompleteWithInformation(
        WdfRequest,
        STATUS_SUCCESS,
        sizeof(*outputBufferPtr));
}

_Use_decl_annotations_
void
PwmIoctlControllerGetActualPeriod (
    const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("()");

    PWM_CONTROLLER_GET_ACTUAL_PERIOD_OUTPUT* outputBufferPtr;
    NTSTATUS status =
        WdfRequestRetrieveOutputBuffer(
            WdfRequest,
            sizeof(*outputBufferPtr),
            reinterpret_cast<PVOID*>(&outputBufferPtr),
            nullptr);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveOutputBuffer(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    outputBufferPtr->ActualPeriod = DeviceContextPtr->ActualPeriod;

    WdfRequestCompleteWithInformation(
        WdfRequest,
        STATUS_SUCCESS,
        sizeof(*outputBufferPtr));
}

_Use_decl_annotations_
void
PwmIoctlControllerSetDesiredPeriod (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("()");

    PWM_CONTROLLER_SET_DESIRED_PERIOD_INPUT* inputBufferPtr;
    NTSTATUS status =
        WdfRequestRetrieveInputBuffer(
            WdfRequest,
            sizeof(*inputBufferPtr),
            reinterpret_cast<PVOID*>(&inputBufferPtr),
            nullptr);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveInputBuffer(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    PWM_CONTROLLER_SET_DESIRED_PERIOD_OUTPUT* outputBufferPtr;
    status =
        WdfRequestRetrieveOutputBuffer(
            WdfRequest,
            sizeof(*outputBufferPtr),
            reinterpret_cast<PVOID*>(&outputBufferPtr),
            nullptr);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveOutputBuffer(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    if ((inputBufferPtr->DesiredPeriod <
            DeviceContextPtr->ControllerInfo.MinimumPeriod) ||
        (inputBufferPtr->DesiredPeriod >
            DeviceContextPtr->ControllerInfo.MaximumPeriod)) {

        PWM_LOG_INFORMATION(
            "DesiredPeriod %llu out of controller limits. "
            "(MinimumPeriod = %llu, MaximumPeriod = %llu)",
            inputBufferPtr->DesiredPeriod,
            DeviceContextPtr->ControllerInfo.MinimumPeriod,
            DeviceContextPtr->ControllerInfo.MaximumPeriod);

        WdfRequestComplete(WdfRequest, STATUS_INVALID_PARAMETER);
        return;
    }

    status =
        PwmSetDesiredPeriod(
            DeviceContextPtr,
            inputBufferPtr->DesiredPeriod);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmSetDesiredPeriod(...) failed. (status = %!STATUS!)",
            status);
        WdfRequestComplete(WdfRequest, status);
        return;
    }

    outputBufferPtr->ActualPeriod = DeviceContextPtr->ActualPeriod;

    WdfRequestCompleteWithInformation(
        WdfRequest,
        STATUS_SUCCESS,
        sizeof(*outputBufferPtr));
}

_Use_decl_annotations_
void
PwmIoctlPinGetActiveDutyCycle (
    const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("(PinNumber = %lu)", PinNumber);

    PWM_PIN_GET_ACTIVE_DUTY_CYCLE_PERCENTAGE_OUTPUT* outputBufferPtr;
    NTSTATUS status =
        WdfRequestRetrieveOutputBuffer(
            WdfRequest,
            sizeof(*outputBufferPtr),
            reinterpret_cast<PVOID*>(&outputBufferPtr),
            nullptr);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveOutputBuffer(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    outputBufferPtr->Percentage = DeviceContextPtr->Pin[PinNumber].ActiveDutyCycle;

    WdfRequestCompleteWithInformation(
        WdfRequest,
        STATUS_SUCCESS,
        sizeof(*outputBufferPtr));
}

_Use_decl_annotations_
void
PwmIoctlPinSetActiveDutyCycle (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("(PinNumber = %lu)", PinNumber);

    PWM_PIN_SET_ACTIVE_DUTY_CYCLE_PERCENTAGE_INPUT* inputBufferPtr;
    NTSTATUS status =
        WdfRequestRetrieveInputBuffer(
            WdfRequest,
            sizeof(*inputBufferPtr),
            reinterpret_cast<PVOID*>(&inputBufferPtr),
            nullptr);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveInputBuffer(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    status =
        PwmSetActiveDutyCycle(
            DeviceContextPtr,
            PinNumber,
            inputBufferPtr->Percentage);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmSetActiveDutyCycle(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    //
    // Some PWM controllers with FIFO can choose to complete this request
    // asynchronously, in which PwmSetActiveDutyCycle should return
    // STATUS_PENDING. The request should be completed in a DPC/Workitem.
    // PwmSetActiveDutyCycle should return STATUS_SUCCESS if the request
    // was completed inline (i.e polling method).
    //
    NT_ASSERT((status == STATUS_PENDING) || (status == STATUS_SUCCESS));

    if (status != STATUS_PENDING) {
        NT_ASSERT(status == STATUS_SUCCESS);
        WdfRequestComplete(WdfRequest, STATUS_SUCCESS);
    }
}

_Use_decl_annotations_
void
PwmIoctlPinGetPolarity (
    const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("(PinNumber = %lu)", PinNumber);

    PWM_PIN_GET_POLARITY_OUTPUT* outputBufferPtr;
    NTSTATUS status =
        WdfRequestRetrieveOutputBuffer(
            WdfRequest,
            sizeof(*outputBufferPtr),
            reinterpret_cast<PVOID*>(&outputBufferPtr),
            nullptr);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveOutputBuffer(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    outputBufferPtr->Polarity = DeviceContextPtr->Pin[PinNumber].Polarity;

    WdfRequestCompleteWithInformation(
        WdfRequest,
        STATUS_SUCCESS,
        sizeof(*outputBufferPtr));
}

_Use_decl_annotations_
void
PwmIoctlPinSetPolarity (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("(PinNumber = %lu)", PinNumber);

    PWM_PIN_SET_POLARITY_INPUT* inputBufferPtr;
    NTSTATUS status =
        WdfRequestRetrieveInputBuffer(
            WdfRequest,
            sizeof(*inputBufferPtr),
            reinterpret_cast<PVOID*>(&inputBufferPtr),
            nullptr);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveInputBuffer(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    switch (inputBufferPtr->Polarity) {
    case PWM_ACTIVE_HIGH:
    case PWM_ACTIVE_LOW:
        break;

    default:
        WdfRequestComplete(WdfRequest, STATUS_INVALID_PARAMETER);
        return;
    }

    PWM_PIN_STATE* pinPtr = DeviceContextPtr->Pin + PinNumber;

    //
    // Setting same polarity is allowed regardless of whether the pin is
    // started or not. But setting polarity while the pin is started is
    // illegal.
    //
    if (pinPtr->Polarity == inputBufferPtr->Polarity) {
        WdfRequestComplete(WdfRequest, STATUS_SUCCESS);
        return;
    } else if (pinPtr->IsStarted) {
        PWM_LOG_ERROR("Changing polarity for a started pin is illegal.");
        WdfRequestComplete(WdfRequest, STATUS_INVALID_DEVICE_STATE);
        return;
    }

    status =
        PwmSetPolarity(
            DeviceContextPtr,
            PinNumber,
            inputBufferPtr->Polarity);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmSetPolarity(...) failed. (status = %!STATUS!)",
            status);
        WdfRequestComplete(WdfRequest, status);
        return;
    }

    WdfRequestComplete(WdfRequest, STATUS_SUCCESS);
}

_Use_decl_annotations_
void
PwmIoctlPinStart (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("(PinNumber = %lu)", PinNumber);

    if (DeviceContextPtr->Pin[PinNumber].IsStarted) {
        WdfRequestComplete(WdfRequest, STATUS_SUCCESS);
        return;
    }

    NTSTATUS status = PwmStart(DeviceContextPtr, PinNumber);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmStart(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    WdfRequestComplete(WdfRequest, STATUS_SUCCESS);
}

_Use_decl_annotations_
void
PwmIoctlPinStop (
    PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("(PinNumber = %lu)", PinNumber);

    if (!DeviceContextPtr->Pin[PinNumber].IsStarted) {
        WdfRequestComplete(WdfRequest, STATUS_SUCCESS);
        return;
    }

    NTSTATUS status = PwmStop(DeviceContextPtr, PinNumber);
    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "PwmStop(...) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    WdfRequestComplete(WdfRequest, STATUS_SUCCESS);
}

_Use_decl_annotations_
void
PwmIoctlPinIsStarted (
    const PWM_DEVICE_CONTEXT* DeviceContextPtr,
    ULONG PinNumber,
    WDFREQUEST WdfRequest
    )
{
    PWM_ASSERT_MAX_IRQL(DISPATCH_LEVEL);
    PWM_LOG_TRACE("(PinNumber = %lu)", PinNumber);

    PWM_PIN_IS_STARTED_OUTPUT* outputBufferPtr;
    NTSTATUS status =
        WdfRequestRetrieveOutputBuffer(
            WdfRequest,
            sizeof(*outputBufferPtr),
            reinterpret_cast<PVOID*>(&outputBufferPtr),
            nullptr);

    if (!NT_SUCCESS(status)) {
        PWM_LOG_ERROR(
            "WdfRequestRetrieveOutputBuffer(..) failed. (status = %!STATUS!)",
            status);

        WdfRequestComplete(WdfRequest, status);
        return;
    }

    outputBufferPtr->IsStarted = DeviceContextPtr->Pin[PinNumber].IsStarted;

    WdfRequestCompleteWithInformation(
        WdfRequest,
        STATUS_SUCCESS,
        sizeof(*outputBufferPtr));
}

PWM_NONPAGED_SEGMENT_END; //=================================================