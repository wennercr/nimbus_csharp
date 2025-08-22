// File: src/Nimbus.Framework/Utils/Logger/ReportManager.cs
using System.Diagnostics;

namespace Nimbus.Framework.Utils.Logger
{
    public static class ReportManager
    {
        public static void GenerateHtmlReport()
        {
            try
            {
                if (bool.TryParse(ConfigLoader.Get("remote"), out var isRemote) && isRemote)
                {
                    Console.WriteLine("[Nimbus] remote=true → skipping local Allure HTML generation.");
                    return;
                }

                var baseDir = AppContext.BaseDirectory; // ...\bin\Debug\netX.Y\
                var results = Path.Combine(baseDir, "target", "allure-results");
                var report = Path.Combine(baseDir, "target", "allure-report");
                var allure = (ConfigLoader.Get("allure.cli.path") ?? "allure").Trim();

                Console.WriteLine($"[Nimbus] OS: {Environment.OSVersion}");
                Console.WriteLine($"[Nimbus] AppContext.BaseDirectory: {baseDir}");
                Console.WriteLine($"[Nimbus] Environment.CurrentDirectory: {Environment.CurrentDirectory}");
                Console.WriteLine($"[Nimbus] Results dir: {results}");
                Console.WriteLine($"[Nimbus] Report  dir: {report}");
                Console.WriteLine($"[Nimbus] Config allure.cli.path: '{allure}'");
                Console.WriteLine($"[Nimbus] PATH: {Environment.GetEnvironmentVariable("PATH")}");

                if (!Directory.Exists(results) || !Directory.EnumerateFileSystemEntries(results).Any())
                {
                    Console.WriteLine("[Nimbus] No results found. Skipping HTML generation.");
                    return;
                }
                Directory.CreateDirectory(report);

                // Print an exact repro you can copy/paste if anything fails
                Console.Error.WriteLine($"[Nimbus] REPRO: cd /d \"{baseDir}\" && {allure} generate \"{results}\" --clean -o \"{report}\"");

                // Use ArgumentList to avoid quoting bugs with cmd.exe
                var psi = new ProcessStartInfo("cmd.exe")
                {
                    WorkingDirectory = baseDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("/c");
                psi.ArgumentList.Add(allure);
                psi.ArgumentList.Add("generate");
                psi.ArgumentList.Add(results);
                psi.ArgumentList.Add("--clean");
                psi.ArgumentList.Add("-o");
                psi.ArgumentList.Add(report);

                using var p = Process.Start(psi)!;
                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();

                if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout);
                if (!string.IsNullOrWhiteSpace(stderr)) Console.Error.WriteLine(stderr);

                Console.WriteLine($"[Nimbus] Allure exit code: {p.ExitCode}");
                if (p.ExitCode == 0)
                    Console.WriteLine($"[Nimbus] Report ready: {Path.Combine(report, "index.html")}");
                else
                    Console.Error.WriteLine("[Nimbus] Allure CLI failed. Copy the REPRO line above and try it in a terminal.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Nimbus] GenerateHtmlReport error: {ex}");
            }
        }


        // Tries to resolve the allure executable as an absolute path (no hardcoding).
        private static string[] ResolveAllure(string cliPref, bool isWin)
        {
            try
            {
                if (isWin)
                {
                    var r = RunProcess("cmd.exe", $"/c where {QuoteIfNeeded(cliPref)}", Environment.CurrentDirectory);
                    if (r.exitCode == 0 && !string.IsNullOrWhiteSpace(r.stdout))
                        return r.stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    // which returns single path or empty
                    var r = RunProcess("/usr/bin/env", $"which {QuoteIfNeeded(cliPref)}", Environment.CurrentDirectory);
                    if (r.exitCode == 0 && !string.IsNullOrWhiteSpace(r.stdout))
                        return new[] { r.stdout.Trim() };
                }
            }
            catch { /* ignore */ }
            return Array.Empty<string>();
        }

        private static string QuoteIfNeeded(string s) =>
            s.Contains(' ') || s.Contains('\t') ? $"\"{s}\"" : s;

        private static (int exitCode, string stdout, string stderr) RunProcess(string fileName, string args, string workingDir)
        {
            var psi = new ProcessStartInfo(fileName, args)
            {
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode, stdout, stderr);
        }
    }
}
