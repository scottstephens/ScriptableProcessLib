using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ScriptableProcessLib;

namespace ScriptableProcessLib.Tests
{
    [TestFixture]
    public class DefaultConfigurationTests
    {
        [Test]
        public void NoImpersonation_Works()
        {
            var process = new ScriptableProcess(false);

            var command = TestChildHelpers.GetConsoleCommand("SpacedOutput");

            var stdout = new StreamServicer(process.Output.Stream, "stdout");
            var stderr = new StreamServicer(process.Error.Stream, "stderr");
            var stdout_task = stdout.RunAsync();
            var stderr_task = stderr.RunAsync();

            process.Start(command);

            process.ProcessEndedGate.WaitOne();

            Task.WaitAll(stdout_task, stderr_task);

            Assert.That(process.ExitCode.HasValue);
            Assert.That(process.ExitCode.Value, Is.EqualTo(1));

            Assert.That(stdout.Output, Is.EqualTo(TestChildHelpers.GetSpacedOutputOutput()));
            Assert.That(stderr.Output, Is.EqualTo(TestChildHelpers.GetSpacedOutputError()));

            // Interestingly, the test process doesn't seem to buffer output for very 
            // long, even without impersonating a console
            //Assert.That(stdout.TimeToFirstRead, Is.GreaterThan(TimeSpan.FromSeconds(0.5)));
            //Assert.That(stderr.TimeToFirstRead, Is.GreaterThan(TimeSpan.FromSeconds(0.5)));
        }

        [Test]
        public void Impersonation_Works()
        {
            var process = new ScriptableProcess(true);

            var command = TestChildHelpers.GetConsoleCommand("SpacedOutput");

            var stdout = new StreamServicer(process.Output.Stream, "stdout");
            var stderr = new StreamServicer(process.Error.Stream, "stderr");
            var stdout_task = stdout.RunAsync();
            var stderr_task = stderr.RunAsync();

            process.Start(command);

            process.ProcessEndedGate.WaitOne();

            Task.WaitAll(stdout_task, stderr_task);

            Assert.That(process.ExitCode.HasValue);
            Assert.That(process.ExitCode.Value, Is.EqualTo(1));

            Assert.That(stdout.Output, Is.EqualTo(TestChildHelpers.GetSpacedOutputOutput()));
            Assert.That(stderr.Output, Is.EqualTo(TestChildHelpers.GetSpacedOutputError()));

            Assert.That(stdout.TimeToFirstRead, Is.LessThan(TimeSpan.FromSeconds(0.5)));
            Assert.That(stderr.TimeToFirstRead, Is.LessThan(TimeSpan.FromSeconds(0.5)));
        }
    }
}
