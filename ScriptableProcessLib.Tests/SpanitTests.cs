using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ScriptableProcessLib.Tests
{
    [TestFixture]
    public class SpanitTests
    {
        [Test]
        [Ignore("Test of program that requires impersonation, but isn't freely distributable.")]
        public void ImpersonationWorks()
        {
            var process = new ScriptableProcess(true);

            var command = @"C:\ProgramFiles\CMEGroup\Span4\bin\spanit.exe C:\Users\scott\Downloads\spanit_example_2.txt";

            var stdout = new StreamServicer(process.Output.Stream, "stdout");
            var stderr = new StreamServicer(process.Error.Stream, "stderr");
            var stdout_task = stdout.RunAsync();
            var stderr_task = stderr.RunAsync();

            process.Start(command);

            process.ProcessEndedGate.WaitOne();

            Task.WaitAll(stdout_task, stderr_task);

            Assert.That(process.ExitCode.HasValue);
            Assert.That(process.ExitCode.Value, Is.EqualTo(0));

            Assert.That(stdout.Output, Is.EqualTo(TestChildHelpers.GetSpacedOutputOutput()));
            Assert.That(stderr.Output, Is.EqualTo(TestChildHelpers.GetSpacedOutputError()));

            Assert.That(stdout.TimeToFirstRead, Is.LessThan(TimeSpan.FromSeconds(0.5)));
        }
    }
}
