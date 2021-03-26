# ScriptableProcessLib
C# tools for starting processes and interacting with them via their standard input, output, and error streams. Windows only at the moment.

# Status
This is a very new library, but I anticipate using it in production in the near future. After it's been in use long enough without any major issues I anticipate creating a Nuget package. Until then, if you're interested in using it, you will need to build from source.

# Building
Requirements
* Windows 10
* .NET 5.0 SDK
* Visual Studio >= 16.9

Instructions
1. Open the ScriptableProcessLib.sln in Visual Studio
2. Build the solution

# Usage

Look at ScriptableProcessLib.Tests/DefaultConfigurationTests.cs for usage examples.

One aspect that may be unclear is what "impersonation" means. One hurdle that exists for communicating with console programs on Windows is that the Visual C Runtime library buffers output directed to stdout unless it detects that the standard output device is a console (sometimes also referred to as a character device). The consequence of this is that if your child process doesn't have a lot of output your controlling process will at best experience delays receiving the output. It can also cause deadlocks if the child process is waiting on input from the parent, which won't provide that input until it receives the buffered output. The impersonation feature uses an undocumented Windows API to make non-console handles appear to MSVCRT as if they were a console.

