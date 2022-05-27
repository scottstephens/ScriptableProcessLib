using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Microsoft.Windows.Sdk;

namespace ScriptableProcessLib
{
    public class ScriptableProcess
    {
        public readonly IInputStream Input;
        public readonly IOutputStream Output;
        public readonly IOutputStream Error;

        public delegate void ProcessEndedDel(ScriptableProcess sender);
        public event ProcessEndedDel ProcessEnded;
        public ManualResetEvent ProcessEndedGate = new ManualResetEvent(false);

        private List<IStandardStream> StreamList;
        private PROCESS_INFORMATION ProcessInfo;

        public uint? ExitCode;

        public ScriptableProcess(bool impersonate_console)
            : this(new InputAnonymousPipe(impersonate_console), new OutputAnonymousPipe(impersonate_console), new OutputAnonymousPipe(impersonate_console))
        {

        }

        public ScriptableProcess(IInputStream input, IOutputStream output, IOutputStream error)
        {
            this.Input = input;
            this.Output = output;
            this.Error = error;
        }

        public void Start(string command)
        {
            STARTUPINFOEXW.Build(out var si);
            STARTUPINFOEXW.AllocateAttributeList(ref si, 1);

            si.StartupInfo.dwFlags |= STARTUPINFOW_dwFlags.STARTF_USESHOWWINDOW;
            si.StartupInfo.wShowWindow = (ushort)SHOW_WINDOW_CMD.SW_HIDE;

            this.StreamList = this.BuildStreamList();
            var handle_list = this.StreamList
                .Select(x => x.ChildHandle)
                .ToArray();
            var flag_list = this.StreamList
                .Select(x => x.ImpersonateConsole ? MSVCRTConstants.FOPEN | MSVCRTConstants.FDEV : 0)
                .ToArray();
            
            STARTUPINFOEXW.SetInheritedHandles(ref si, handle_list, flag_list);

            if (this.Input != null)
                si.StartupInfo.hStdInput = new HANDLE(this.Input.DangerousChildHandle);
            if (this.Output != null)
                si.StartupInfo.hStdOutput = new HANDLE(this.Output.DangerousChildHandle);
            if (this.Error != null)
                si.StartupInfo.hStdError = new HANDLE(this.Error.DangerousChildHandle);

            if (this.StreamList.Count > 0)
                si.StartupInfo.dwFlags |= STARTUPINFOW_dwFlags.STARTF_USESTDHANDLES;

            ProcessHelpers.CreateProcess(out this.ProcessInfo, in si, command, inherit_handles: true);

            foreach (var stream in this.StreamList)
                stream.HandleProcessStart();

            var wh = new SafeWaitHandle(this.ProcessInfo.hProcess.Value, ownsHandle: false);
            var mre = new ManualResetEvent(false)
            {
                SafeWaitHandle = wh,
            };
            ThreadPool.RegisterWaitForSingleObject(mre, HandleProcessEnded, this, -1, true);
        }

        private List<IStandardStream> BuildStreamList()
        {
            var output = new List<IStandardStream>(3);
            if (this.Input != null)
                output.Add(this.Input);
            if (this.Output != null && this.Output != this.Input)
                output.Add(this.Output);
            if (this.Error != null && this.Error != this.Input && this.Error != this.Output)
                output.Add(this.Error);

            return output;
        }

        private static void HandleProcessEnded(object state, bool timed_out)
        {
            var p = (ScriptableProcess)state;
            p.HandleProcessEnded();
        }

        private void HandleProcessEnded()
        {
            var process_handle = new SafeProcessHandle(this.ProcessInfo.hProcess.Value, false);
            bool exit_code_success = PInvoke.GetExitCodeProcess(process_handle, out uint exit_code);

            if (exit_code_success)
                this.ExitCode = exit_code;

            foreach (var stream in this.StreamList)
                stream.HandleProcessEnd();

            this.ProcessEndedGate.Set();
            this.ProcessEnded?.Invoke(this);
        }
    }
}
