using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ScriptableProcessLib.Tests
{
    public class StreamServicer
    {
        public Stream Stream;
        public string Label;

        public TimeSpan TimeToFirstRead;

        public StringBuilder OutputBuilder = new StringBuilder();

        public string Output => this.OutputBuilder.ToString();

        public StreamServicer(Stream stream, string label)
        {
            this.Stream = stream;
            this.Label = label;
        }

        public void Run()
        {
            var reader = new StreamReader(this.Stream);
            bool first = true;
            var sw = new Stopwatch();
            sw.Start();
            int line_count = 0;
            while (true)
            {
                var line = reader.ReadLine();
                line_count += 1;

                if (line == null) // signifies end of stream
                    break;

                if (first)
                {
                    sw.Stop();
                    this.TimeToFirstRead = sw.Elapsed;
                }

                this.OutputBuilder.AppendLine(line);
            }
        }

        public Task RunAsync()
        {
            return Task.Factory.StartNew(this.Run, TaskCreationOptions.LongRunning);
        }
    }
}
