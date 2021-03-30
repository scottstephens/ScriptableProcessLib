using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Windows.Sdk
{
    public unsafe partial struct STARTUPINFOEXW
    {
        public static void AllocateAttributeList(ref STARTUPINFOEXW self, uint attribute_count)
        {
            var lpSize = UIntPtr.Zero;
            var success = PInvoke.InitializeProcThreadAttributeList(
                lpAttributeList: IntPtr.Zero,
                dwAttributeCount: attribute_count,
                dwFlags: 0,
                lpSize: out lpSize
            );
            if (success || lpSize == UIntPtr.Zero) // we're not expecting `success` here, we just want to get the calculated lpSize
            {
                throw new InvalidOperationException("Could not calculate the number of bytes for the attribute list. " + Marshal.GetLastWin32Error());
            }

            self.lpAttributeList = Marshal.AllocHGlobal((int)lpSize);

            success = PInvoke.InitializeProcThreadAttributeList(
                lpAttributeList: self.lpAttributeList,
                dwAttributeCount: attribute_count,
                dwFlags: 0,
                lpSize: out lpSize
            );
            if (!success)
            {
                throw new InvalidOperationException("Could not set up attribute list. " + Marshal.GetLastWin32Error());
            }
        }

        public static void Build(out STARTUPINFOEXW output)
        {
            output = new STARTUPINFOEXW();
            output.StartupInfo.cb = (uint)sizeof(STARTUPINFOEXW);
        }

        public static void SetInheritedHandles(ref STARTUPINFOEXW startupInfo, SafeHandle[] handles_to_inherit, uint[] flags_to_inherit = null)
        {
            // this method implements the behavior described in https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#preparing-for-creation-of-the-child-process

            if (flags_to_inherit != null && flags_to_inherit.Length != handles_to_inherit.Length)
                throw new ArgumentException("If flags_to_inherit is non-null, it must have the same length as handles_to_inherit");

            var handle_list_size = handles_to_inherit.Length * Marshal.SizeOf<IntPtr>();
            var handle_list = Marshal.AllocHGlobal(handle_list_size);
            for (int ii = 0; ii < handles_to_inherit.Length; ++ii)
            {
                var dest = handle_list + Marshal.SizeOf<IntPtr>() * ii;
                Marshal.StructureToPtr(handles_to_inherit[ii].DangerousGetHandle(), dest, false);
            }

            bool success = PInvoke.UpdateProcThreadAttribute(
                lpAttributeList: startupInfo.lpAttributeList,
                dwFlags: 0,
                Attribute: (UIntPtr)0x20002, //(UIntPtr)PROC_THREAD_ATTRIBUTE_NUM.ProcThreadAttributeHandleList,
                lpValue: (void*)handle_list,
                cbSize: (UIntPtr)handle_list_size,
                lpPreviousValue: null,
                lpReturnSize: (UIntPtr*)null
            );

            if (!success)
            {
                var err = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failure setting the HANDLE_LIST thread attribute. Error code: {err} / 0x{err:x}");
            }

            if (flags_to_inherit != null)
            {
                for (int ii = 0; ii < handles_to_inherit.Length; ++ii)
                {
                    PInvoke.SetHandleInformation(handles_to_inherit[ii], (uint)HANDLE_FLAG_OPTIONS.HANDLE_FLAG_INHERIT, HANDLE_FLAG_OPTIONS.HANDLE_FLAG_INHERIT);
                }
                var buffer_len = (ushort)(4 + 1 * flags_to_inherit.Length + Marshal.SizeOf<IntPtr>() * flags_to_inherit.Length);

                var buffer_ptr = (byte*)Marshal.AllocHGlobal(buffer_len);
                *(uint*)buffer_ptr = (uint)flags_to_inherit.Length;
                for (int ii = 0; ii < flags_to_inherit.Length; ++ii)
                {
                    *(buffer_ptr + 4 + ii) = (byte)flags_to_inherit[ii];
                }

                for (int ii = 0; ii < flags_to_inherit.Length; ++ii)
                {
                    Marshal.StructureToPtr(handles_to_inherit[ii].DangerousGetHandle(), (IntPtr)(buffer_ptr + 4 + flags_to_inherit.Length + sizeof(IntPtr) * ii), false);
                }

                startupInfo.StartupInfo.cbReserved2 = buffer_len;
                startupInfo.StartupInfo.lpReserved2 = buffer_ptr;
            }
        }
    }
}
