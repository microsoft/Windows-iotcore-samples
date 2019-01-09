/*++

Copyright (c) Microsoft Corporation.  All rights reserved.

    THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
    PURPOSE.

Module Name:

    filter.h

Abstract:

    Contains structure definitions and function prototypes for the
    HIDUSB lower filter driver that supports the Waveshare 7" HDMI
    LCD touchscreen device (Rev 2.1)

Environment:

    Kernel mode

--*/

#include <ntddk.h>
#include <wdf.h>
#include <wdmsec.h> // for SDDLs
#define NTSTRSAFE_LIB
#include <ntstrsafe.h>
#include <usb.h>
#if !defined(_FILTER_H_)
#define _FILTER_H_


#define DRIVERNAME "WaveshareFilter.sys: "

#define WAVESHARE_REPORT_ID 0x02
#define CONFIDENCE_BIT 0x04

typedef struct _FILTER_EXTENSION
{
    WDFDEVICE WdfDevice;

}FILTER_EXTENSION, *PFILTER_EXTENSION;


WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(FILTER_EXTENSION,
                                        FilterGetData)

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD FilterEvtDeviceAdd;
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL FilterEvtIoDeviceControl;

VOID
FilterForwardRequestWithCompletionRoutine(
    IN WDFREQUEST Request,
    IN WDFIOTARGET Target
    );

VOID
FilterRequestCompletionRoutine(
    IN WDFREQUEST                  Request,
    IN WDFIOTARGET                 Target,
    PWDF_REQUEST_COMPLETION_PARAMS CompletionParams,
    IN WDFCONTEXT                  Context
   );


#endif

