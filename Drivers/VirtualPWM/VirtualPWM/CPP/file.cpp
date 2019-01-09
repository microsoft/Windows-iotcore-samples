//
// Copyright (C) Microsoft. All rights reserved.
//
// Module Name:
//
//   file.cpp
//
// Abstract:
//
//  This module contains methods implementation for the file object create/close
//  callbacks.
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
#include "file.tmh"

PWM_PAGED_SEGMENT_BEGIN; //==================================================

_Use_decl_annotations_
VOID
PwmEvtDeviceFileCreate (
    WDFDEVICE WdfDevice,
    WDFREQUEST WdfRequest,
    WDFFILEOBJECT WdfFileObject
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    PWM_LOG_TRACE("()");

    UNICODE_STRING* filenamePtr = WdfFileObjectGetFileName(WdfFileObject);
    PWM_DEVICE_CONTEXT* deviceContextPtr = PwmGetDeviceContext(WdfDevice);
    NTSTATUS status;
    ULONG pinNumber = ULONG_MAX;

    //
    // Parse and validate the filename associated with the file object.
    //
    bool isPinInterface;
    if (filenamePtr == nullptr) {
        WdfRequestComplete(WdfRequest, STATUS_INVALID_DEVICE_REQUEST);
        return;
    } else if (filenamePtr->Length > 0) {
        //
        // A non-empty filename means to open a pin under the controller namespace.
        //
        status = PwmParsePinPath(filenamePtr, &pinNumber);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "Failed to parse pin path. (status = %!STATUS!)",
                status);
            WdfRequestComplete(WdfRequest, status);
            return;
        }

        if (pinNumber >= deviceContextPtr->ControllerInfo.PinCount) {
            PWM_LOG_INFORMATION(
                "Requested pin number out of bounds. (pinNumber = %lu)",
                pinNumber);

            WdfRequestComplete(WdfRequest, STATUS_NO_SUCH_FILE);
            return;
        }

        isPinInterface = true;
    } else {
        //
        // An empty filename means that the create is against the root controller.
        //
        isPinInterface = false;
    }

    ACCESS_MASK desiredAccess;
    ULONG shareAccess;
    PwmCreateRequestGetAccess(WdfRequest, &desiredAccess, &shareAccess);

    //
    // ShareAccess will not be honored as it has no meaning currently in the
    // PWM DDI.
    //
    if (shareAccess != 0) {
        PWM_LOG_INFORMATION(
            "Requested share access is not supported and request ShareAccess "
            "parameter should be zero. Access denied. (shareAccess = %lu)",
            shareAccess);

        WdfRequestComplete(WdfRequest, STATUS_SHARING_VIOLATION);
        return;
    }

    //
    // Verify request desired access.
    //
    const bool hasWriteAccess = ((desiredAccess & FILE_WRITE_DATA) != 0);

    if (isPinInterface) {
        PWM_PIN_STATE* pinPtr = deviceContextPtr->Pin + pinNumber;

        WdfWaitLockAcquire(pinPtr->Lock, NULL);
         
        if (hasWriteAccess) {
            if (pinPtr->IsOpenForWrite) {
                WdfWaitLockRelease(pinPtr->Lock);
                PWM_LOG_ERROR("Pin%lu access denied.", pinNumber);
                WdfRequestComplete(WdfRequest, STATUS_SHARING_VIOLATION);
                return;
            }
            pinPtr->IsOpenForWrite = true;
        }

        PWM_LOG_INFORMATION(
            "Pin%lu Opened. (IsOpenForWrite = %!bool!)",
            pinNumber,
            pinPtr->IsOpenForWrite);

        WdfWaitLockRelease(pinPtr->Lock);

    } else {

        WdfWaitLockAcquire(deviceContextPtr->ControllerLock, NULL);

        if (hasWriteAccess) {
            if (deviceContextPtr->IsControllerOpenForWrite) {
                WdfWaitLockRelease(deviceContextPtr->ControllerLock);
                PWM_LOG_ERROR("Controller access denied.");
                WdfRequestComplete(WdfRequest, STATUS_SHARING_VIOLATION);
                return;
            }
            deviceContextPtr->IsControllerOpenForWrite = true;
        }

        PWM_LOG_INFORMATION(
            "Controller Opened. (IsControllerOpenForWrite = %!bool!)",
            deviceContextPtr->IsControllerOpenForWrite);

        WdfWaitLockRelease(deviceContextPtr->ControllerLock);
    }

    //
    // Allocate and fill a file object context.
    //
    PWM_FILE_OBJECT_CONTEXT* fileObjectContextPtr;
    {
        WDF_OBJECT_ATTRIBUTES wdfObjectAttributes;
        WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(
            &wdfObjectAttributes,
            PWM_FILE_OBJECT_CONTEXT);

        void* contextPtr;
        status = WdfObjectAllocateContext(
                WdfFileObject,
                &wdfObjectAttributes,
                &contextPtr);
        if (!NT_SUCCESS(status)) {
            PWM_LOG_ERROR(
                "WdfObjectAllocateContext(...) failed. (status = %!STATUS!)",
                status);

            WdfRequestComplete(WdfRequest, status);
            return;
        }

        fileObjectContextPtr =
            static_cast<PWM_FILE_OBJECT_CONTEXT*>(contextPtr);

        NT_ASSERT(fileObjectContextPtr != nullptr);
        fileObjectContextPtr->IsPinInterface = isPinInterface;
        fileObjectContextPtr->IsOpenForWrite = hasWriteAccess;
        if (isPinInterface) {
            fileObjectContextPtr->PinNumber = pinNumber;
        } else {
            //
            // A special value to indicate invalid pin number.
            //
            fileObjectContextPtr->PinNumber = ULONG_MAX;
        }
    }

    WdfRequestComplete(WdfRequest, STATUS_SUCCESS);
}

_Use_decl_annotations_
VOID
PwmEvtFileClose (
    WDFFILEOBJECT WdfFileObject
    )
{
    PAGED_CODE();
    PWM_ASSERT_MAX_IRQL(PASSIVE_LEVEL);

    PWM_LOG_TRACE("()");

    WDFDEVICE wdfDevice = WdfFileObjectGetDevice(WdfFileObject);
    PWM_DEVICE_CONTEXT* deviceContextPtr = PwmGetDeviceContext(wdfDevice);
    PWM_FILE_OBJECT_CONTEXT* fileObjectContextPtr = PwmGetFileObjectContext(WdfFileObject);

    if (fileObjectContextPtr->IsPinInterface) {
        const ULONG pinNumber = fileObjectContextPtr->PinNumber;
        PWM_PIN_STATE* pinPtr = deviceContextPtr->Pin + pinNumber;

        WdfWaitLockAcquire(pinPtr->Lock, NULL);

        if (fileObjectContextPtr->IsOpenForWrite) {

            NTSTATUS status = PwmResetPinDefaults(deviceContextPtr, pinNumber);
            if (!NT_SUCCESS(status)) {
                PWM_LOG_ERROR(
                    "PwmResetPinDefaults(...) failed. (status = %!STATUS!)",
                    status);
            }

            NT_ASSERT(pinPtr->IsOpenForWrite);
            pinPtr->IsOpenForWrite = false;
        }

        PWM_LOG_TRACE(
            "Pin%lu Closed. (IsOpenForWrite = %lu)",
            pinNumber,
            (pinPtr->IsOpenForWrite ? 1 : 0));

        WdfWaitLockRelease(pinPtr->Lock);

    } else {

        WdfWaitLockAcquire(deviceContextPtr->ControllerLock, NULL);

        if (fileObjectContextPtr->IsOpenForWrite) {
            NTSTATUS status = PwmResetControllerDefaults(deviceContextPtr);
            if (!NT_SUCCESS(status)) {
                PWM_LOG_ERROR(
                    "PwmResetControllerDefaults(...) failed. (status = %!STATUS!)",
                    status);
            }

            NT_ASSERT(deviceContextPtr->IsControllerOpenForWrite);
            deviceContextPtr->IsControllerOpenForWrite = false;
        }

        PWM_LOG_TRACE(
            "Controller Closed. (IsControllerOpenForWrite = %lu)",
            (deviceContextPtr->IsControllerOpenForWrite ? 1 : 0));

        WdfWaitLockRelease(deviceContextPtr->ControllerLock);
    }
}

PWM_PAGED_SEGMENT_END; //===================================================