using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace ScriptableProcessLib
{
    public class OutputAnonymousPipe : IOutputStream
    {
        private AnonymousPipeServerStream Inner;
        public bool ImpersonateConsole { get; protected set; }

        private OutputAnonymousPipe() { }

        public OutputAnonymousPipe(bool impersonate_console)
        {
            this.Inner = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            this.ImpersonateConsole = impersonate_console;
        }

        public SafeHandle ChildHandle => this.Inner.ClientSafePipeHandle;

        public IntPtr DangerousChildHandle => this.Inner.ClientSafePipeHandle.DangerousGetHandle();

        public Stream Stream => this.Inner;

        public void HandleProcessStart()
        {
            this.Inner.DisposeLocalCopyOfClientHandle();
        }

        public void HandleProcessEnd()
        {

        }

        
    }
}
