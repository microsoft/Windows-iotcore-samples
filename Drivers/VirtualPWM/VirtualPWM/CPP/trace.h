//
// Copyright (C) Microsoft.  All rights reserved.
//

#ifndef _TRACE_H_
#define _TRACE_H_

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

//
// Defining control guids, including this is required to happen before
// including the tmh file (if the WppRecorder API is used)
//
#include <WppRecorder.h>

//
// Tracing GUID - {E2BDF62D-48DA-4195-B31C-F47D1AB8015C}
//
#define WPP_CONTROL_GUIDS \
    WPP_DEFINE_CONTROL_GUID(VIRTUALPWM, (E2BDF62D,48DA,4195,B31C,F47D1AB8015C), \
        WPP_DEFINE_BIT(PWM_TRACING_DEFAULT) \
    )

// begin_wpp config
//
// FUNC PWM_LOG_ERROR{LEVEL=TRACE_LEVEL_ERROR, FLAGS=PWM_TRACING_DEFAULT}(MSG, ...);
// USEPREFIX (PWM_LOG_ERROR, "%!STDPREFIX! [%s @ %u] ERROR :", __FILE__, __LINE__);
//
// FUNC PWM_LOG_LOW_MEMORY{LEVEL=TRACE_LEVEL_ERROR, FLAGS=PWM_TRACING_DEFAULT}(MSG, ...);
// USEPREFIX (PWM_LOG_LOW_MEMORY, "%!STDPREFIX! [%s @ %u] LOW MEMORY :", __FILE__, __LINE__);
//
// FUNC PWM_LOG_WARNING{LEVEL=TRACE_LEVEL_WARNING, FLAGS=PWM_TRACING_DEFAULT}(MSG, ...);
// USEPREFIX (PWM_LOG_WARNING, "%!STDPREFIX! [%s @ %u] WARNING :", __FILE__, __LINE__);
//
// FUNC PWM_LOG_INFORMATION{LEVEL=TRACE_LEVEL_INFORMATION, FLAGS=PWM_TRACING_DEFAULT}(MSG, ...);
// USEPREFIX (PWM_LOG_INFORMATION, "%!STDPREFIX! [%s @ %u] INFO :", __FILE__, __LINE__);
//
// FUNC PWM_LOG_TRACE{LEVEL=TRACE_LEVEL_VERBOSE, FLAGS=PWM_TRACING_DEFAULT}(MSG, ...);
// USEPREFIX (PWM_LOG_TRACE, "%!STDPREFIX! [%s @ %u] TRACE :", __FILE__, __LINE__);
//
// end_wpp

#ifdef __cplusplus
} // extern "C"
#endif // __cplusplus

#endif // _TRACE_H_