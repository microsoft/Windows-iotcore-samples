/*++

Copyright (c) Microsoft Corporation.  All rights reserved.

    THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
    PURPOSE.

Module Name:

    filter.c

Abstract:

    This module implements a HIDUSB lower filter driver to support the Waveshare 7"
    HDMI LCD touchscreen device (Rev 2.1).

Environment:

    Kernel mode

--*/

#include "filter.h"


#ifdef ALLOC_PRAGMA
#pragma alloc_text (INIT, DriverEntry)
#pragma alloc_text (PAGE, FilterEvtDeviceAdd)
#endif


NTSTATUS
DriverEntry(
    IN PDRIVER_OBJECT  DriverObject,
    IN PUNICODE_STRING RegistryPath
    )
/*++

Routine Description:

    Installable driver initialization entry point.
    This entry point is called directly by the I/O system.

Arguments:

    DriverObject - pointer to the driver object

    RegistryPath - pointer to a unicode string representing the path,
                   to driver-specific key in the registry.

Return Value:

    STATUS_SUCCESS if successful,
    STATUS_UNSUCCESSFUL otherwise.

--*/
{
    WDF_DRIVER_CONFIG   config;
    NTSTATUS            status;
    WDFDRIVER           hDriver;

    //
    // Initiialize driver config to control the attributes that
    // are global to the driver. Note that framework by default
    // provides a driver unload routine. If you create any resources
    // in the DriverEntry and want to be cleaned in driver unload,
    // you can override that by manually setting the EvtDriverUnload in the
    // config structure. In general xxx_CONFIG_INIT macros are provided to
    // initialize most commonly used members.
    //

    WDF_DRIVER_CONFIG_INIT(
        &config,
        FilterEvtDeviceAdd
    );

    //
    // Create a framework driver object to represent our driver.
    //
    status = WdfDriverCreate(DriverObject,
                            RegistryPath,
                            WDF_NO_OBJECT_ATTRIBUTES,
                            &config,
                            &hDriver);
    if (!NT_SUCCESS(status)) {
        KdPrint( ("WdfDriverCreate failed with status 0x%x\n", status));
    }

    return status;
}


NTSTATUS
FilterEvtDeviceAdd(
    IN WDFDRIVER        Driver,
    IN PWDFDEVICE_INIT  DeviceInit
    )
/*++
Routine Description:

    EvtDeviceAdd is called by the framework in response to AddDevice
    call from the PnP manager.

Arguments:

    Driver - Handle to a framework driver object created in DriverEntry

    DeviceInit - Pointer to a framework-allocated WDFDEVICE_INIT structure.

Return Value:

    NTSTATUS

--*/
{
    WDF_OBJECT_ATTRIBUTES   deviceAttributes;
    NTSTATUS                status;
    WDFDEVICE               device;
    WDF_IO_QUEUE_CONFIG     ioQueueConfig;

    PAGED_CODE ();

    UNREFERENCED_PARAMETER(Driver);

    //
    // Tell the framework that you are filter driver. Framework
    // takes care of inherting all the device flags & characterstics
    // from the lower device you are attaching to.
    //
    WdfFdoInitSetFilter(DeviceInit);

    //
    // Specify the size of device extension where we track per device
    // context.
    //

    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&deviceAttributes, FILTER_EXTENSION);

    //
    // Create a framework device object.This call will inturn create
    // a WDM deviceobject, attach to the lower stack and set the
    // appropriate flags and attributes.
    //
    status = WdfDeviceCreate(&DeviceInit, &deviceAttributes, &device);
    if (!NT_SUCCESS(status)) {
        KdPrint( ("WdfDeviceCreate failed with status code 0x%x\n", status));
        return status;
    }

    //
    // Configure the default queue to be Parallel.
    //
    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&ioQueueConfig,
                             WdfIoQueueDispatchParallel);

    //
    // Framework by default creates non-power managed queues for
    // filter drivers.
    //
    ioQueueConfig.EvtIoInternalDeviceControl = FilterEvtIoDeviceControl;

    status = WdfIoQueueCreate(device,
                            &ioQueueConfig,
                            WDF_NO_OBJECT_ATTRIBUTES,
                            WDF_NO_HANDLE // pointer to default queue
                            );
    if (!NT_SUCCESS(status)) {
        KdPrint( ("WdfIoQueueCreate failed 0x%x\n", status));
        return status;
    }

    return status;
}

VOID
FilterEvtIoDeviceControl(
    IN WDFQUEUE      Queue,
    IN WDFREQUEST    Request,
    IN size_t        OutputBufferLength,
    IN size_t        InputBufferLength,
    IN ULONG         IoControlCode
    )
/*++

Routine Description:

    This routine is the dispatch routine for internal device control requests.

Arguments:

    Queue - Handle to the framework queue object that is associated
            with the I/O request.
    Request - Handle to a framework request object.

    OutputBufferLength - length of the request's output buffer,
                        if an output buffer is available.
    InputBufferLength - length of the request's input buffer,
                        if an input buffer is available.

    IoControlCode - the driver-defined or system-defined I/O control code
                    (IOCTL) that is associated with the request.

Return Value:

   VOID

--*/
{
    UNREFERENCED_PARAMETER(OutputBufferLength);
    UNREFERENCED_PARAMETER(InputBufferLength);
    UNREFERENCED_PARAMETER(IoControlCode);
    WDFDEVICE                       device;

    device = WdfIoQueueGetDevice(Queue);

    //
    // Set up post processing of the IOCTL IRP.
    //
    FilterForwardRequestWithCompletionRoutine(Request,
                                            WdfDeviceGetIoTarget(device));

    return;
}

VOID
FilterForwardRequestWithCompletionRoutine(
    IN WDFREQUEST Request,
    IN WDFIOTARGET Target
    )
/*++
Routine Description:

    This routine forwards the request to a lower driver with
    a completion so that when the request is completed by the
    lower driver, it can regain control of the request and look
    at the result.

--*/
{
    BOOLEAN ret;
    NTSTATUS status;

    //
    // The following funciton essentially copies the content of
    // current stack location of the underlying IRP to the next one.
    //
    WdfRequestFormatRequestUsingCurrentType(Request);

    WdfRequestSetCompletionRoutine(Request,
                                FilterRequestCompletionRoutine,
                                WDF_NO_CONTEXT);

    ret = WdfRequestSend(Request,
                         Target,
                         WDF_NO_SEND_OPTIONS);

    if (ret == FALSE) {
        status = WdfRequestGetStatus (Request);
        KdPrint( ("WdfRequestSend failed: 0x%x\n", status));
        WdfRequestComplete(Request, status);
    }

    return;
}

VOID
FilterRequestCompletionRoutine(
    IN WDFREQUEST                  Request,
    IN WDFIOTARGET                 Target,
    PWDF_REQUEST_COMPLETION_PARAMS CompletionParams,
    IN WDFCONTEXT                  Context
   )
/*++

Routine Description:

    Completion Routine.  Adjust the HID report data for the device.

Arguments:

    Target - Target handle
    Request - Request handle
    Params - request completion params
    Context - Driver supplied context


Return Value:

    VOID

--*/
{
    UNREFERENCED_PARAMETER(Target);
    UNREFERENCED_PARAMETER(Context);

    PURB pUrb;
    PIRP pIrp;
    PIO_STACK_LOCATION pStack;
    PUCHAR pBuff;
    UINT16 bufLen;

    pIrp = WdfRequestWdmGetIrp(Request);
    pStack = IoGetCurrentIrpStackLocation(pIrp);

    pUrb = (PURB)pStack->Parameters.Others.Argument1;

    if (pUrb != NULL) {
        switch (pUrb->UrbHeader.Function) {
            case URB_FUNCTION_BULK_OR_INTERRUPT_TRANSFER :
                //
                // HID report from the waveshare device does not set the
                // confidence bit when finger is lifted, so we modify the
                // report to set this bit.
                //
                pBuff = (PUCHAR)pUrb->UrbBulkOrInterruptTransfer.TransferBuffer;
                bufLen = (UINT16)pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength;

                //
                // Waveshare touchscreen HID input report layout:
                // (As of Rev2.1 Firmware.  Subject to change by Manufacturer)
                //
                // Each HID input report contains contact information for 2 contacts.
                // Each contact generates 3 input reports (for a total of 5 contacts)
                //
                // Length = 14 bytes
                //
                // Layout:
                //  Byte 0: Report Id
                //  Contact Info:
                //      Byte 1: Bitmap
                //          bit 0 - Tip switch
                //          bit 1 - In range
                //          bit 2 - Touch Valid / Confidence
                //      Byte 2: Contact Identifier (Finger number)
                //      Byte 3-4: X position
                //      Byte 5-6: Y position
                //  Contact Info:
                //      Byte 7: Bitmap:
                //          bit 0 - Tip switch
                //          bit 1 - In range
                //          bit 2 - Touch Valid / Confidence
                //      Byte 8: Contact Identifier (Finger number)
                //      Byte 9-10: X position
                //      Byte 11-12: Y position
                //  Byte 13: Contact count
                //
                if ((bufLen == 0xe) && (pBuff != NULL)) {
                    if (pBuff[0] == WAVESHARE_REPORT_ID) {
                        //
                        // This is an input HID report from the touchscreen.
                        // Each HID report reports two fingers of data.
                        // Set confidence bit if needed.
                        //
                        if ((pBuff[1] & CONFIDENCE_BIT) == 0) {
                            pBuff[1] |= CONFIDENCE_BIT;
                        }
                        if ((pBuff[7] & CONFIDENCE_BIT) == 0) {
                            pBuff[7] |= CONFIDENCE_BIT;
                        }
                    }
                }
            break;
            default:
            break;
        }
    }

    WdfRequestComplete(Request, CompletionParams->IoStatus.Status);

    return;
}




