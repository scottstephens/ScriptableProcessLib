using System;
using System.Threading;

namespace ScriptableProcessLib.TestChildConsole
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args[0] == "SpacedOutput")
                return SpacedOutput.Run(args);
            else if (args[0] == "StdinTest")
                return StdinTest.Run(args);
            else if (args[0] == "DelayTaskExample")
                return DelayTaskExample.Run(args);
            else if (args[0] == "OnlyOutput")
                return OnlyOutput.Run(args);
            else
                return SpacedOutput.Run(args);
        }
    }
}
