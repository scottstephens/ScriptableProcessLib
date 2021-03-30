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

        [Test]
        public void Inherit3Pipes_SwapOutErr()
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            var stdout = new OutputAnonymousPipe(false);
            var stdout_servicer = new StreamServicer(stdout.Stream, "Stdout");
            var stdout_task = stdout_servicer.RunAsync();

            var stderr = new OutputAnonymousPipe(false);
            var stderr_servicer = new StreamServicer(stderr.Stream, "Stderr");
            var stderr_task = stderr_servicer.RunAsync();

            var stdin = new InputAnonymousPipe(false);

            var handle_list = new SafeHandle[] { stdin.ChildHandle, stderr.ChildHandle, stdout.ChildHandle };
            var flags_to_inherit = new uint[] { FOPEN | FDEV, FOPEN | FDEV, FOPEN | FDEV };
            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, flags_to_inherit);

            si.StartupInfo.hStdOutput = new HANDLE(stdout.ChildHandle.DangerousGetHandle());
            si.StartupInfo.hStdError = new HANDLE(stderr.ChildHandle.DangerousGetHandle());
            si.StartupInfo.dwFlags = STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            var command = TestChildHelpers.GetConsoleCommand("SpacedOutput");
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            stdout.HandleProcessStart();
            stderr.HandleProcessStart();
            stdin.HandleProcessStart();

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            child_process.WaitForExit();

            var process_handle = new SafeProcessHandle(pi.hProcess.Value, false);
            bool exit_code_success = PInvoke.GetExitCodeProcess(process_handle, out uint exit_code);

            stdout_task.Wait();
            stderr_task.Wait();

            var stdout_content = stdout_servicer.Output;
            var stderr_content = stderr_servicer.Output;

            Assert.That(exit_code_success, Is.True);
            Assert.That(exit_code, Is.EqualTo(1));
            Assert.That(stdout_content, Is.Not.Empty);

            child_process.Dispose();
            stdout.Stream.Dispose();
            stderr.Stream.Dispose();
            stdin.Stream.Dispose();
        }

        [Test]
        public void Inherit3Pipes_SwapOutErr_OnlyOutput()
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            var stdout = new OutputAnonymousPipe(false);
            var stdout_servicer = new StreamServicer(stdout.Stream, "Stdout");
            var stdout_task = stdout_servicer.RunAsync();

            var stderr = new OutputAnonymousPipe(false);
            var stderr_servicer = new StreamServicer(stderr.Stream, "Stderr");
            var stderr_task = stderr_servicer.RunAsync();

            var stdin = new InputAnonymousPipe(false);

            var handle_list = new SafeHandle[] { stdin.ChildHandle, stderr.ChildHandle, stdout.ChildHandle };
            var flags_to_inherit = new uint[] { FOPEN | FDEV, FOPEN | FDEV, FOPEN | FDEV };
            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, flags_to_inherit);

            si.StartupInfo.hStdOutput = new HANDLE(stdout.ChildHandle.DangerousGetHandle());
            si.StartupInfo.hStdError = new HANDLE(stderr.ChildHandle.DangerousGetHandle());
            si.StartupInfo.dwFlags = STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            var command = TestChildHelpers.GetConsoleCommand("OnlyOutput");
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            stdout.HandleProcessStart();
            stderr.HandleProcessStart();
            stdin.HandleProcessStart();

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            child_process.WaitForExit();

            var process_handle = new SafeProcessHandle(pi.hProcess.Value, false);
            bool exit_code_success = PInvoke.GetExitCodeProcess(process_handle, out uint exit_code);

            stdout_task.Wait();
            stderr_task.Wait();

            var stdout_content = stdout_servicer.Output;
            var stderr_content = stderr_servicer.Output;

            Assert.That(exit_code_success, Is.True);
            Assert.That(exit_code, Is.EqualTo(0));
            Assert.That(stdout_content, Is.Not.Empty);

            child_process.Dispose();
            stdout.Stream.Dispose();
            stderr.Stream.Dispose();
            stdin.Stream.Dispose();
        }
    }
}
