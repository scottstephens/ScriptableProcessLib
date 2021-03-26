using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptableProcessLib.TestChildConsole
{
    public static class ConsoleHelper
    {
        public static string ReadLine(TimeSpan timeout)
        {
            return ReadLine(Task.Delay(timeout));
        }

        public static string ReadLine(Task cancel_trigger)
        {
            var status = new Status();

            var cancel_task = Task.Run(async () =>
            {
                await cancel_trigger;

                status.Mutex.WaitOne();
                bool io_done = status.IODone;
                if (!io_done)
                    status.CancellationStarted = true;
                status.Mutex.ReleaseMutex();

                while (!status.IODone)
                {
                    var success = CancelStdIn(out int error_code);

                    if (!success && error_code != 0x490) // 0x490 is what happens when you call cancel and there is not a pending I/O request
                        throw new Exception($"Canceling IO operation on StdIn failed with error {error_code} ({error_code:x})");
                }
            });

            ReadLineWithStatus(out string input, out bool read_canceled);
            
            if (!read_canceled)
            {
                status.Mutex.WaitOne();
                bool must_wait = status.CancellationStarted;
                status.IODone = true;
                status.Mutex.ReleaseMutex();

                if (must_wait)
                    cancel_task.Wait();

                return input;
            }
            else // read_canceled == true
            {
                status.Mutex.WaitOne();
                bool cancel_started = status.CancellationStarted;
                status.IODone = true;
                status.Mutex.ReleaseMutex();

                if (!cancel_started)
                    throw new Exception("Received cancellation not triggered by this method.");
                else
                    cancel_task.Wait();

                return null;
            }
        }

        private const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);


        private static bool CancelStdIn(out int error_code)
        {
            var handle = GetStdHandle(STD_INPUT_HANDLE);
            bool success = CancelIoEx(handle, IntPtr.Zero);

            if (success)
            {
                error_code = 0;
                return true;
            }
            else
            {
                var rc = Marshal.GetLastWin32Error();
                error_code = rc;
                return false;
            }
        }

        private class Status
        {
            public Mutex Mutex = new Mutex(false);
            public volatile bool IODone;
            public volatile bool CancellationStarted;
        }

        private static void ReadLineWithStatus(out string result, out bool operation_canceled)
        {
            try
            {
                result = Console.ReadLine();
                operation_canceled = false;
            }
            catch (OperationCanceledException)
            {
                result = null;
                operation_canceled = true;
            }
        }
    }
}
