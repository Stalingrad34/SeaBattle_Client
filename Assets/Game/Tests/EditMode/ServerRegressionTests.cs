using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public sealed class ServerRegressionTests
    {
        [UnityTest]
        public IEnumerator ServerRulesAndNetworkRegressions()
        {
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Server"));
            Assert.That(File.Exists(Path.Combine(directory, "node_modules/typescript/bin/tsc")), Is.True,
                "Place the server repository in Server and run npm ci first.");
            yield return RunNode(directory, "node_modules/typescript/bin/tsc", 30);
            yield return RunNode(directory, "--test tests/*.test.mjs", 150);
        }

        private static IEnumerator RunNode(string directory, string arguments, int timeoutSeconds)
        {
            var output = new StringBuilder();
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node", Arguments = arguments, WorkingDirectory = directory,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                }
            };
            void Capture(object sender, DataReceivedEventArgs args)
            {
                if (args.Data == null)
                    return;
                lock (output)
                    output.AppendLine(args.Data);
            }
            process.OutputDataReceived += Capture;
            process.ErrorDataReceived += Capture;
            Assert.That(process.Start(), Is.True, "Node.js 22+ must be available on PATH.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var timer = Stopwatch.StartNew();
            try
            {
                while (!process.HasExited && timer.Elapsed.TotalSeconds < timeoutSeconds)
                    yield return null;
                Assert.That(process.HasExited, Is.True, "Server regression test timed out.");
                process.WaitForExit();
                string result;
                lock (output)
                    result = output.ToString();
                Assert.That(process.ExitCode, Is.Zero, result);
                TestContext.WriteLine(result);
            }
            finally
            {
                if (!process.HasExited)
                    process.Kill();
            }
        }
    }
}
