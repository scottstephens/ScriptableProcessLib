using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ScriptableProcessLib
{
    public interface IStandardStream
    {
        SafeHandle ChildHandle { get; }
        IntPtr DangerousChildHandle { get; }
        Stream Stream { get; }
        bool ImpersonateConsole { get; }
        void HandleProcessStart();
        void HandleProcessEnd();
    }
}
