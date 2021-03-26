using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace ScriptableProcessLib.Tests
{
    public static class TestChildHelpers
    {
        public static string GetConsoleCommand(string subprogram)
        {
            var exe = Path.Combine(TestContext.CurrentContext.TestDirectory, "ScriptableProcessLib.TestChildConsole.exe");
            var command = $"{exe} {subprogram}";
            return command;
        }

        public static string GetSpacedOutputOutput()
        {
            var builder = new StringBuilder();
            builder.AppendLine("stdout: 1");
            builder.AppendLine("stdout: 2");
            builder.AppendLine("stdout: 3");
            return builder.ToString();
        }

        public static string GetSpacedOutputError()
        {
            var builder = new StringBuilder();
            builder.AppendLine("stderr: 1");
            builder.AppendLine("stderr: 2");
            builder.AppendLine("stderr: 3");
            return builder.ToString();
        }

        public static string GetSpacedOutputBoth()
        {
            var builder = new StringBuilder();
            builder.AppendLine("stdout: 1");
            builder.AppendLine("stderr: 1");
            builder.AppendLine("stdout: 2");
            builder.AppendLine("stderr: 2");
            builder.AppendLine("stdout: 3");
            builder.AppendLine("stderr: 3");
            return builder.ToString();
        }
    }
}
