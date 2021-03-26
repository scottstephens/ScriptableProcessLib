using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Microsoft.Windows.Sdk;
using NUnit.Framework;
using static ScriptableProcessLib.MSVCRTConstants;

namespace ScriptableProcessLib.Tests
{
    [TestFixture]
    public class ProcThreadAttributeHandleListTests
    {
        [Test]
        public void Inherit1Pipe_No_UseStdHandles_NoRedirect()
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            var stdin = new InputAnonymousPipe(true);
            var stdin_writer = new StreamWriter(stdin.Stream, leaveOpen:true);

            var handle_list = new SafeHandle[] { stdin.ChildHandle };
            var flags_to_inherit = new uint[] { FOPEN | FDEV };
            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, flags_to_inherit);

            var command = TestChildHelpers.GetConsoleCommand("SpacedOutput");
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            stdin_writer.WriteLine("test");
            stdin_writer.Flush();

            child_process.WaitForExit();

            var process_handle = new SafeProcessHandle(pi.hProcess.Value, false);
            bool exit_code_success = PInvoke.GetExitCodeProcess(process_handle, out uint exit_code);

            Assert.That(exit_code_success, Is.True);

            Assert.That(exit_code, Is.EqualTo(1));

            child_process.Dispose();
            stdin.Stream.Dispose();
        }

        [Test]
        public void Inherit1Pipe_SetStdInput_Redirects_Input()
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            var stdin = new InputAnonymousPipe(true);
            var stdin_writer = new StreamWriter(stdin.Stream, leaveOpen: true);

            var handle_list = new SafeHandle[] { stdin.ChildHandle };
            var flags_to_inherit = new uint[] { FOPEN | FDEV };
            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, flags_to_inherit);

            si.StartupInfo.hStdInput = new HANDLE(stdin.ChildHandle.DangerousGetHandle());
            si.StartupInfo.dwFlags = STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            var command = TestChildHelpers.GetConsoleCommand("SpacedOutput");
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            stdin_writer.WriteLine("test");
            stdin_writer.Flush();

            child_process.WaitForExit();

            var process_handle = new SafeProcessHandle(pi.hProcess.Value, false);
            bool exit_code_success = PInvoke.GetExitCodeProcess(process_handle, out uint exit_code);

            Assert.That(exit_code_success, Is.True);

            Assert.That(exit_code, Is.EqualTo(0));

            child_process.Dispose();
            stdin.Stream.Dispose();
        }
    }
}
