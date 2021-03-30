using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Microsoft.Windows.Sdk;
using NUnit.Framework;
using static ScriptableProcessLib.MSVCRTConstants;

namespace ScriptableProcessLib.Tests
{
    public class SpanitBase
    {
        private string SpanitExe;

        private string[] SpanitSearchPath = new string[]
        {
            @"C:\ProgramFiles\CMEGroup\Span4\bin\spanit.exe",
            @"C:\Span4\bin\spanit.exe",
        };

        private string getSpanit()
        {
            foreach (var candidate in SpanitSearchPath)
            {
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        protected string getCommand(string arg)
        {
            return $"{this.SpanitExe} {arg}";
        }

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            this.SpanitExe = getSpanit();

            if (this.SpanitExe == null)
                Assert.Ignore("Requires CME Group's spanit program, which is not installed (and is not freely distributable).");
        }

        protected string PipeName;
        protected NamedPipeServerStream PipeStream;
        protected string PipePath;

        public void initializePipe()
        {
            this.PipeName = Guid.NewGuid().ToString();
            this.PipeStream = new NamedPipeServerStream(this.PipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            this.PipePath = @"\\.\pipe\" + this.PipeName;
        }

        [SetUp]
        public void SetUp()
        {
            this.initializePipe();
        }

        [TearDown]
        public void TearDown()
        {
            this.PipeStream.Close();
        }
    }

    [TestFixture]
    public class SpanitTests : SpanitBase
    {
        [Test]
        public void NoImpersonationFails()
        {
            var process = new ScriptableProcess(false);

            var stdout = new StreamServicer(process.Output.Stream, "stdout");
            var stderr = new StreamServicer(process.Error.Stream, "stderr");
            var stdout_task = stdout.RunAsync();
            var stderr_task = stderr.RunAsync();

            var command = this.getCommand(this.PipePath);
            process.Start(command);

            this.PipeStream.WaitForConnection();
            var writer = new StreamWriter(this.PipeStream, leaveOpen: false);
            Thread.Sleep(1000);
            writer.Write("Print DateTime");
            writer.Close();

            process.ProcessEndedGate.WaitOne();

            Task.WaitAll(stdout_task, stderr_task);

            Assert.That(process.ExitCode.HasValue);
            Assert.That(process.ExitCode.Value, Is.EqualTo(0));

            Assert.That(stdout.TimeToFirstRead.TotalSeconds, Is.GreaterThan(0.5));
            Assert.That(stdout.Output, Is.Not.Empty);
        }

        [Test]
        public void ImpersonationWorks()
        {
            var process = new ScriptableProcess(true);

            var stdout = new StreamServicer(process.Output.Stream, "stdout");
            var stderr = new StreamServicer(process.Error.Stream, "stderr");
            var stdout_task = stdout.RunAsync();
            var stderr_task = stderr.RunAsync();

            var command = this.getCommand(this.PipePath);
            process.Start(command);

            this.PipeStream.WaitForConnection();
            var writer = new StreamWriter(this.PipeStream, leaveOpen: false);
            Thread.Sleep(1000);
            writer.Write("Print DateTime");
            writer.Close();

            process.ProcessEndedGate.WaitOne();

            Task.WaitAll(stdout_task, stderr_task);

            Assert.That(process.ExitCode.HasValue);
            Assert.That(process.ExitCode.Value, Is.EqualTo(0));

            Assert.That(stdout.TimeToFirstRead.TotalSeconds, Is.LessThan(0.5));
            Assert.That(stdout.Output, Is.Not.Empty);
        }
    }

    [TestFixture]
    [Ignore("These are not really tests, just documentation of unexpected behavior in the spanit program.")]
    // Should be Explicit instead of ignore, but Visual Studio / NUnitTestAdapter don't handle Explit properly:
    // https://github.com/nunit/nunit3-vs-adapter/issues/658
    public class SpanitCharacterization : SpanitBase
    {
        [Test]
        public void Spanit_Test()
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            var stdout = new OutputAnonymousPipe(false);
            var stdout_servicer = new StreamServicer(stdout.Stream, "Stdout");
            var stdout_task = stdout_servicer.RunAsync();

            var handle_list = new SafeHandle[] { stdout.ChildHandle };

            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, null);

            si.StartupInfo.hStdOutput = new HANDLE(stdout.ChildHandle.DangerousGetHandle());
            si.StartupInfo.dwFlags = STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            var command = this.getCommand(this.PipePath);
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            stdout.HandleProcessStart();

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            this.PipeStream.WaitForConnection();
            var writer = new StreamWriter(this.PipeStream, leaveOpen: false);
            writer.Write("Print DateTime");
            writer.Close();

            child_process.WaitForExit();

            var process_handle = new SafeProcessHandle(pi.hProcess.Value, false);
            bool exit_code_success = PInvoke.GetExitCodeProcess(process_handle, out uint exit_code);

            stdout_task.Wait();

            var stdout_content = stdout_servicer.Output;

            Assert.That(exit_code_success, Is.True);
            Assert.That(exit_code, Is.EqualTo(0));
            Assert.That(stdout_content, Is.Not.Empty);

            child_process.Dispose();
            stdout.Stream.Dispose();
        }

        [Test]
        public void Spanit_Test2()
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            var stdout = new OutputAnonymousPipe(false);
            var stdout_servicer = new StreamServicer(stdout.Stream, "Stdout");
            var stdout_task = stdout_servicer.RunAsync();

            var stderr = new OutputAnonymousPipe(false);
            var stderr_servicer = new StreamServicer(stderr.Stream, "Stderr");
            var stderr_task = stderr_servicer.RunAsync();

            var handle_list = new SafeHandle[] { stdout.ChildHandle, stderr.ChildHandle };

            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, null);

            si.StartupInfo.hStdOutput = new HANDLE(stdout.ChildHandle.DangerousGetHandle());
            si.StartupInfo.hStdError = new HANDLE(stderr.ChildHandle.DangerousGetHandle());
            si.StartupInfo.dwFlags = STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            var command = this.getCommand(this.PipePath);
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            stdout.HandleProcessStart();
            stderr.HandleProcessStart();

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            this.PipeStream.WaitForConnection();
            var writer = new StreamWriter(this.PipeStream, leaveOpen: false);
            writer.Write("Print DateTime");
            writer.Close();

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
        }

        [Test]
        public void Spanit_Test3()
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            var stdout = new OutputAnonymousPipe(false);
            var stdout_servicer = new StreamServicer(stdout.Stream, "Stdout");
            var stdout_task = stdout_servicer.RunAsync();

            var stderr = new OutputAnonymousPipe(false);
            var stderr_servicer = new StreamServicer(stderr.Stream, "Stderr");
            var stderr_task = stderr_servicer.RunAsync();

            var handle_list = new SafeHandle[] { stderr.ChildHandle, stdout.ChildHandle };

            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, null);

            si.StartupInfo.hStdOutput = new HANDLE(stdout.ChildHandle.DangerousGetHandle());
            si.StartupInfo.hStdError = new HANDLE(stderr.ChildHandle.DangerousGetHandle());
            si.StartupInfo.dwFlags = STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            var command = this.getCommand(this.PipePath);
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            stdout.HandleProcessStart();
            stderr.HandleProcessStart();

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            this.PipeStream.WaitForConnection();
            var writer = new StreamWriter(this.PipeStream, leaveOpen: false);
            writer.Write("Print DateTime");
            writer.Close();

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
        }

        [Test]
        public void Spanit_Test4()
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            var stdout = new OutputAnonymousPipe(false);
            var stdout_servicer = new StreamServicer(stdout.Stream, "Stdout");
            var stdout_task = stdout_servicer.RunAsync();

            var stderr = new OutputAnonymousPipe(false);
            var stderr_servicer = new StreamServicer(stderr.Stream, "Stderr");
            var stderr_task = stderr_servicer.RunAsync();

            var handle_list = new SafeHandle[] { stderr.ChildHandle, stdout.ChildHandle };
            var flags_to_inherit = new uint[] { FOPEN | FDEV, FOPEN | FDEV };
            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, flags_to_inherit);

            si.StartupInfo.hStdOutput = new HANDLE(stdout.ChildHandle.DangerousGetHandle());
            si.StartupInfo.hStdError = new HANDLE(stderr.ChildHandle.DangerousGetHandle());
            si.StartupInfo.dwFlags = STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            var command = this.getCommand(this.PipePath);
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            stdout.HandleProcessStart();
            stderr.HandleProcessStart();

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            this.PipeStream.WaitForConnection();
            var writer = new StreamWriter(this.PipeStream, leaveOpen: false);
            writer.Write("Print DateTime");
            writer.Close();

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
        }

        // This is one that demonstrates the issue
        [Test]
        public void Spanit_Test5()
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

            var command = this.getCommand(this.PipePath);
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            stdout.HandleProcessStart();
            stderr.HandleProcessStart();
            stdin.HandleProcessStart();

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            this.PipeStream.WaitForConnection();
            var writer = new StreamWriter(this.PipeStream, leaveOpen: false);
            writer.Write("Print DateTime");
            writer.Close();

            child_process.WaitForExit();

            var process_handle = new SafeProcessHandle(pi.hProcess.Value, false);
            bool exit_code_success = PInvoke.GetExitCodeProcess(process_handle, out uint exit_code);

            stdout_task.Wait();
            stderr_task.Wait();

            var stdout_content = stdout_servicer.Output;
            var stderr_content = stderr_servicer.Output;

            Assert.That(exit_code_success, Is.True);
            Assert.That(exit_code, Is.EqualTo(0));

            // Note that stderr_content has what we would expect to be in stdout_content
            Assert.That(stdout_content, Is.Not.Empty);

            child_process.Dispose();
            stdout.Stream.Dispose();
            stderr.Stream.Dispose();
            stdin.Stream.Dispose();
        }

        // also fails
        [Test]
        public void Spanit_Test6()
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
            si.StartupInfo.hStdInput = new HANDLE(stdin.ChildHandle.DangerousGetHandle());
            si.StartupInfo.dwFlags = STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            var command = this.getCommand(this.PipePath);
            ProcessHelpers.CreateProcess(out PROCESS_INFORMATION pi, in si, command, inherit_handles: true);

            stdout.HandleProcessStart();
            stderr.HandleProcessStart();
            stdin.HandleProcessStart();

            var child_process = Process.GetProcessById((int)pi.dwProcessId);

            this.PipeStream.WaitForConnection();
            var writer = new StreamWriter(this.PipeStream, leaveOpen: false);
            writer.Write("Print DateTime");
            writer.Close();

            child_process.WaitForExit();

            var process_handle = new SafeProcessHandle(pi.hProcess.Value, false);
            bool exit_code_success = PInvoke.GetExitCodeProcess(process_handle, out uint exit_code);

            stdout_task.Wait();
            stderr_task.Wait();

            var stdout_content = stdout_servicer.Output;
            var stderr_content = stderr_servicer.Output;

            Assert.That(exit_code_success, Is.True);
            Assert.That(exit_code, Is.EqualTo(0));

            // Note that stderr_content has what we would expect to be in stdout_content
            Assert.That(stdout_content, Is.Not.Empty);

            child_process.Dispose();
            stdout.Stream.Dispose();
            stderr.Stream.Dispose();
            stdin.Stream.Dispose();
        }
    }
}
