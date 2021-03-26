using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Windows.Sdk;

namespace ScriptableProcessLib
{
    public static unsafe class ProcessHelpers
    {
        public static void CreateProcess(out PROCESS_INFORMATION pi, in STARTUPINFOEXW sInfoEx, string commandLine, bool inherit_handles=false)
        {
            var securityAttributeSize = (uint)Marshal.SizeOf<SECURITY_ATTRIBUTES>();
            var pSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
            var tSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };

            bool success = false;
            fixed (char* command_line_ptr = commandLine)
            {
                success = PInvoke.CreateProcessW(
                    lpApplicationName: null,
                    lpCommandLine: command_line_ptr,
                    lpProcessAttributes: (SECURITY_ATTRIBUTES?)pSec,
                    lpThreadAttributes: (SECURITY_ATTRIBUTES?)tSec,
                    bInheritHandles: inherit_handles,
                    dwCreationFlags: PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT,
                    lpEnvironment: null,
                    lpCurrentDirectory: null,
                    lpStartupInfo: in sInfoEx.StartupInfo,
                    lpProcessInformation: out pi
                );
            }

            if (!success)
            {
                throw new InvalidOperationException("Could not create process. " + Marshal.GetLastWin32Error());
            }
        }
    }
}
