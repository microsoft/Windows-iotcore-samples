//
// Copyright (C) Microsoft. All rights reserved.
//
// Module Name:
//
//   utility.hpp
//
// Abstract:
//
//   This header contains driver helper routines.
//
// Environment:
//
//  Kernel mode only
//

#ifndef _UTILITY_H_
#define _UTILITY_H_

//
// In safe-arithmetic, perform integer division (LeftAddend + RightAddend)/Divisor
// and return the result rounded up or down to the nearest integer where 3.5 and
// 3.75 are near 4, while 3.25 is near 3.
//
// LeftAddend: First 64-bit unsigned integer addend.
// RightAddend: Second 64-bit unsigned integer addend.
// ResultPtr: A pointer to a 64-bit unsigned integer that receives the result.
//
// Returns error in case of overflow, otherwise returns STATUS_SUCCESS.
//
__forceinline
NTSTATUS
SafeAddDivRound64x64x64 (
    _In_ ULONGLONG LeftAddend,
    _In_ ULONGLONG RightAddend,
    _In_ ULONGLONG Divisor,
    _Out_ ULONGLONG* ResultPtr)
{
    ASSERT(ARGUMENT_PRESENT(ResultPtr));
    ASSERT(Divisor > 0);

    //
    // Calculate the following in safe-arithmetic to avoid overflows:
    // return ((LeftAddend + RightAddend) + (Divisor / 2)) / Divisor;
    //

    ULONGLONG dividend;
    NTSTATUS status = RtlULongLongAdd(LeftAddend, RightAddend, &dividend);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    ULONGLONG result;
    status = RtlULongLongAdd(dividend, Divisor / 2, &result);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    *ResultPtr = result / Divisor;

    return STATUS_SUCCESS;
}

//
// Return the result of Dividend / Divisor rounded up or down to the nearest intege
// where 3.5 and 3.75 are near 4, while 3.25 is near 3. Note that the implementation
// is subject to overflow if the intermediate value of adding Dividend + (Divisor / 2)
// overflows 64-bit. Use only if you are sure that an overflow is not possible.
//
// Dividend: A 64-bit unsigned integer dividend.
// Divisor: A 64-bit unsigned integer divisor.
//
// Returns the rounded division 64-bit result
//
__forceinline
ULONGLONG
UnsafeDivRound64x64 (
    _In_ ULONGLONG Dividend,
    _In_ ULONGLONG Divisor
    )
{
    ASSERT(Divisor > 0);
    return (Dividend + (Divisor / 2)) / Divisor;
}

//
// In safe-arithmetic, perform integer division Dividend/Divisor and return the
// result rounded up or down to the nearest integer where 3.5 and 3.75 are near
// 4, while 3.25 is near 3.
//
// Dividend: A 64-bit unsigned integer dividend.
// Divisor: A 64-bit unsigned integer divisor.
// ResultPtr: A pointer to a 64-bit unsigned integer that receives the result.
//
// Returns error in case of overflow, otherwise returns STATUS_SUCCESS.
//
__forceinline
NTSTATUS
SafeDivRound64x64 (
    _In_ ULONGLONG Dividend,
    _In_ ULONGLONG Divisor,
    _Out_ ULONGLONG* ResultPtr
    )
{
    ASSERT(ARGUMENT_PRESENT(ResultPtr));
    ASSERT(Divisor > 0);

    //
    // Calculate the following in safe-arithmetic to avoid overflows:
    // return (Dividend + (Divisor / 2)) / Divisor;
    //

    ULONGLONG result;
    NTSTATUS status = RtlULongLongAdd(Dividend, Divisor / 2, &result);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    *ResultPtr = result / Divisor;

    return STATUS_SUCCESS;
}

//
// In safe-arithmetic, perform a multiplication followed by division in the form
// (Mul64 * Mul32) / Div64 where the result is rounded to the nearest integer.
//
// Mul64: A 64-bit unsigned integer multiplicand.
// Mul32: A 32-bit unsigned multiplier.
// Div64: A 64-bit unsigned integer divisor.
// ResultPtr: A pointer to a 64-bit unsigned integer that receives the result.
//
// Returns error in case of overflow, otherwise returns STATUS_SUCCESS.
//
__forceinline
NTSTATUS
SafeMulDivRound64x32x64 (
    _In_ ULONGLONG Mul64,
    _In_ ULONG Mul32,
    _In_ ULONGLONG Div64,
    _Out_ ULONGLONG* Result64Ptr
    )
{
    ASSERT(ARGUMENT_PRESENT(Result64Ptr));

    ULONGLONG q = Mul64 / Div64;
    ULONGLONG r = Mul64 % Div64;
    NTSTATUS status;

    //
    // Calculate the following in safe-arithmetic to avoid overflows:
    // return (q * Mul32) + ((r * Mul32) / Div64);
    //

    ULONGLONG qMul32;
    status = RtlULongLongMult(q, Mul32, &qMul32);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    ULONGLONG rMul32;
    status = RtlULongLongMult(r, Mul32, &rMul32);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    ULONGLONG rMul32OverDiv64;
    status = SafeDivRound64x64(rMul32, Div64, &rMul32OverDiv64);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    ULONGLONG result;
    status = RtlULongLongAdd(qMul32, rMul32OverDiv64, &result);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    *Result64Ptr = result;

    return STATUS_SUCCESS;
}

__forceinline
ULONGLONG
PwmPeriodToFrequency(
    _In_ PWM_PERIOD DesiredPeriod
    )
{
    return UnsafeDivRound64x64(PICOSECONDS_IN_1_SECOND, DesiredPeriod);
}

__forceinline
ULONGLONG
PwmFrequencyToPeriod(
    _In_ ULONGLONG Frequency
    )
{
    return UnsafeDivRound64x64(PICOSECONDS_IN_1_SECOND, Frequency);
}

__forceinline
void
PwmCreateRequestGetAccess(
    _In_ WDFREQUEST WdfRequest,
    _Out_ ACCESS_MASK* DesiredAccessPtr,
    _Out_ ULONG* ShareAccessPtr
    )
{
    NT_ASSERT(ARGUMENT_PRESENT(DesiredAccessPtr));
    NT_ASSERT(ARGUMENT_PRESENT(ShareAccessPtr));

    WDF_REQUEST_PARAMETERS wdfRequestParameters;
    WDF_REQUEST_PARAMETERS_INIT(&wdfRequestParameters);
    WdfRequestGetParameters(WdfRequest, &wdfRequestParameters);

    NT_ASSERTMSG(
        "Expected create request",
        wdfRequestParameters.Type == WdfRequestTypeCreate);

    *DesiredAccessPtr =
        wdfRequestParameters.Parameters.Create.SecurityContext->DesiredAccess;
    *ShareAccessPtr = wdfRequestParameters.Parameters.Create.ShareAccess;
}

#endif