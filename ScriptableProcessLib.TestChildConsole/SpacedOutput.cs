using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptableProcessLib.TestChildConsole
{
    public class SpacedOutput
    {
        public static int Run(string[] args)
        {
            try
            {
                WriteOutputLineWithTimeout("stdout: 1", TimeSpan.FromSeconds(1));
                WriteErrorLineWithTimeout("stderr: 1", TimeSpan.FromSeconds(1));
                
                for (int ii = 2; ii < 4; ++ii)
                {
                    WriteOutputLineWithTimeout($"stdout: {ii}", TimeSpan.FromSeconds(1));
                    WriteErrorLineWithTimeout($"stderr: {ii}", TimeSpan.FromSeconds(1));

                    Thread.Sleep(1000);
                }

                var input = ReadStdinLineWithTimeout(TimeSpan.FromSeconds(1));

                return 0;
            }
            catch (StdoutTimeout)
            {
                return 2;
            }
            catch(StderrTimeout)
            {
                return 3;
            }
            catch(StdinTimeout)
            {
                return 1;
            }
        }

        public static void WriteOutputLineWithTimeout(string content, TimeSpan timeout)
        {
            var cancel_source = new CancellationTokenSource(timeout);
            var write_task = Console.Out.WriteLineAsync(content.AsMemory(), cancel_source.Token);
            write_task.Wait();
            if (!write_task.IsCompleted)
                throw new StdoutTimeout();
        }

        public static void WriteErrorLineWithTimeout(string content, TimeSpan timeout)
        {
            var cancel_source = new CancellationTokenSource(timeout);
            var write_task = Console.Error.WriteLineAsync(content.AsMemory(), cancel_source.Token);
            write_task.Wait();
            if (!write_task.IsCompletedSuccessfully)
                throw new StderrTimeout();
        }

        public static string ReadStdinLineWithTimeout(TimeSpan timeout)
        {
            var line = ConsoleHelper.ReadLine(timeout);

            if (line == null)
                throw new StdinTimeout();
            else
                return line;
        }

        public class StdoutTimeout : Exception
        {
            public StdoutTimeout() : base("A write to stdout timed out.") { }
        }

        public class StderrTimeout : Exception
        {
            public StderrTimeout() : base("A write to stderr timed out.") { }
        }

        public class StdinTimeout : Exception
        {
            public StdinTimeout() : base("A read from stdin timed out.") { }
        }
    }
}
