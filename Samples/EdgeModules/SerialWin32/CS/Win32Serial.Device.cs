//
// Copyright (c) Microsoft. All rights reserved.
//
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Win32Serial
{
    public class Device: IDisposable
    {
        #region Static Methods

        public static string[] EnumerateDevices()
        {
            uint size;
            uint status;
            Guid SerialGuid = new Guid("86E0D1E0-8089-11D0-9CE4-08003E301F73");
            status = CM_Get_Device_Interface_List_Size(out size, ref SerialGuid, null, 0);

            char[] buffer = new char[size];
            CM_Get_Device_Interface_List(ref SerialGuid, null, buffer, size, 0);

            String DeviceList = new String(buffer);
            var devicestrings = DeviceList.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

            return devicestrings;
        }

        public static Device Create(string id)
        {
            var result = new Device(id);

            result.SetupComm(512, 512);

            return result;
        }

        public static NativeOverlapped CreateOverlapped()
        {
            IntPtr overlappedEvent = CreateEvent(IntPtr.Zero, true, false, null);
            NativeOverlapped overlapped = new NativeOverlapped();
            overlapped.EventHandle = overlappedEvent;

            return overlapped;
        }

        #endregion

        #region Constructor

        protected Device(string id)
        {
            devicehandle = CreateFile(id,
                 0x80000000 | 0x40000000, // GENERIC_READ | GENERIC_WRITE
                 0,
                 IntPtr.Zero,
                 3, // OPEN_EXISTING
                 0x80 | 0x40000000, // FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED
                 IntPtr.Zero);

            if (devicehandle.IsInvalid)
                throw new ApplicationException($"Unable to open UART {id}");
        }

        #endregion

        #region Properties

        public DCB Config
        {
            get
            {
                var result = new DCB();
                var ok = GetCommState(devicehandle, ref result);
                if (!ok)
                    throw new ApplicationException("Unable to query UART configuration");
                return result;
            }
            set
            {
                var ok = SetCommState(devicehandle, ref value);
                if (!ok)
                    throw new ApplicationException("Unable to set new UART configuration");
            }
        }
        public COMMTIMEOUTS Timeouts
        {
            get
            {
                var result = new COMMTIMEOUTS();
                var ok = GetCommTimeouts(devicehandle, ref result);
                if (!ok)
                    throw new ApplicationException("Unable to query UART timeout values");
                return result;
            }
            set
            {
                var ok = SetCommTimeouts(devicehandle, ref value);
                if (!ok)
                    throw new ApplicationException("Unable to set UART timeout values");
            }
        }

        public string[] Info
        {
            get
            {
                var result = new List<string>();

                var config = Config;
                foreach (var field in config.GetType().GetFields())
                {
                    result.Add($"{field.Name}: 0x{Convert.ToInt32(field.GetValue(config)):X}");
                }

                var timeouts = Timeouts;
                foreach (var field in timeouts.GetType().GetFields())
                {
                    result.Add($"{field.Name}: 0x{Convert.ToInt32(field.GetValue(timeouts)):X}");
                }

                return result.ToArray();
            }
        }

        #endregion

        #region Methods

        public void SetupComm(uint inqueuesize, uint outqueuesize)
        {
            // Configure device queue size
            var ok = SetupComm(devicehandle, inqueuesize, outqueuesize);
            if (!ok)
                throw new ApplicationException("Unable to setup UART device queues");
        }

        public bool Read(byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref NativeOverlapped lpOverlapped)
        {
            return ReadFile(devicehandle, lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead, ref lpOverlapped);
        }

        public bool Write(byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, ref NativeOverlapped lpOverlapped)
        {
            return WriteFile(devicehandle, lpBuffer, nNumberOfBytesToWrite, out lpNumberOfBytesWritten, ref lpOverlapped);
        }

        public bool GetOverlappedResult(ref NativeOverlapped lpOverlapped, out uint lpNumberOfBytesTransferred, bool bWait)
        {
            return GetOverlappedResult(devicehandle, ref lpOverlapped, out lpNumberOfBytesTransferred, bWait);
        }

        #endregion

        #region Internal Properties

        private SafeFileHandle devicehandle = null;

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // No managed state (managed objects) to dispose
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                CloseHandle(devicehandle);

                disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Device() {
           // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
           Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Configuration Structs

        [StructLayout(LayoutKind.Sequential)]
        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/ns-winbase-_dcb
        public struct DCB
        {
            public int DCBlength;   // sizeof(DCB)
            public int BaudRate;    // current baud rate
            public uint flags;      /* these are the c struct bit fields, bit twiddle flag to set
                                     public int fBinary; // binary mode, no EOF check
                                     public int fParity; // enable parity checking
                                     public int fOutxCtsFlow; // CTS output flow control
                                     public int fOutxDsrFlow; // DSR output flow control
                                     public int fDtrControl; // DTR flow control type
                                     public int fDsrSensitivity; // DSR sensitivity
                                     public int fTXContinueOnXoff; // XOFF continues Tx
                                     public int fOutX; // XON/XOFF out flow control
                                     public int fInX; // XON/XOFF in flow control
                                     public int fErrorChar; // enable error replacement
                                     public int fNull; // enable null stripping
                                     public int fRtsControl; // RTS flow control
                                     public int fAbortOnError; // abort on error
                                     public int fDummy2; // reserved
                                     */
            public ushort wReserved;
            public ushort XonLim;   // transmit XON threshold
            public ushort XoffLim;  // transmit XOFF threshold
            public byte ByteSize;   // number of bits/byte, 4-8
            public byte Parity;     // 0-4=no,odd,even,mark,space
            public byte StopBits;   // 0,1,2 = 1, 1.5, 2
            public char XonChar;    // Tx and Rx XON character
            public char XoffChar;   // Tx and Rx XOFF character
            public char ErrorChar;  // error replacement character
            public char EofChar;    // end of input character
            public char EvtChar;    // received event character
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential)]
        // https://docs.microsoft.com/en-us/windows/desktop/api/winbase/ns-winbase-_commtimeouts
        public struct COMMTIMEOUTS
        {
            public UInt32 ReadIntervalTimeout;
            public UInt32 ReadTotalTimeoutMultiplier;
            public UInt32 ReadTotalTimeoutConstant;
            public UInt32 WriteTotalTimeoutMultiplier;
            public UInt32 WriteTotalTimeoutConstant;
        }

        #endregion

        #region Platform Invokes

        [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
        static extern uint CM_Get_Device_Interface_List_Size(out uint size, ref Guid interfaceClassGuid, string deviceID, uint flags);
        [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
        static extern uint CM_Get_Device_Interface_List(ref Guid interfaceClassGuid, string deviceID, char[] buffer, uint bufferLength, uint flags);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
            uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern void CloseHandle(SafeHandle handle);
        [DllImport("kernel32.dll")]
        static extern bool SetupComm(SafeFileHandle hFile, uint dwInQueue, uint dwOutQueue);
        [DllImport("kernel32.dll")]
        static extern bool GetCommState(SafeFileHandle hFile, ref DCB lpDCB);
        [DllImport("kernel32.dll")]
        static extern bool SetCommState(SafeFileHandle hFile, ref DCB lpDCB);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetCommTimeouts(SafeFileHandle hFile, ref COMMTIMEOUTS lpCommTimeouts);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetCommTimeouts(SafeFileHandle hFile, ref COMMTIMEOUTS lpCommTimeouts);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadFile(SafeFileHandle hFile, [Out] byte[] lpBuffer,
            uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, [In] ref System.Threading.NativeOverlapped lpOverlapped);
        [DllImport("kernel32.dll")]
        static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer,
            uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, [In] ref System.Threading.NativeOverlapped lpOverlapped);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetOverlappedResult(SafeFileHandle hFile, [In] ref System.Threading.NativeOverlapped lpOverlapped,
            out uint lpNumberOfBytesTransferred, bool bWait);

        #endregion
    }
}
