using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Nimbus.Framework.Utils
{
    /// <summary>
    /// Download helper with three modes:
    ///  - Local: poll a local downloads folder (unchanged)
    ///  - Remote + Grid (non-Selenoid): Selenium Managed Downloads (unchanged)
    ///  - Remote + Selenoid: poll a host-mounted folder (NEW), no Managed Downloads APIs
    /// </summary>
    public class DownloadHelper
    {
        private readonly IWebDriver driver;
        private readonly DirectoryInfo downloadDir;
        private readonly bool isRemote;
        private readonly bool isSelenoid;

        public DownloadHelper(IWebDriver driver)
        {
            this.driver = driver ?? throw new ArgumentNullException(nameof(driver));

            this.isRemote = bool.TryParse(ConfigLoader.Get("remote"), out var r) && r;
            this.isSelenoid = bool.TryParse(ConfigLoader.Get("isSelenoid"), out var s) && s;

            if (isRemote && isSelenoid)
            {
                // Host-mounted folder (GitHub Actions): passed as selenoidDownloadHostDir
                var hostDir = ConfigLoader.Get("selenoidDownloadHostDir");
                if (string.IsNullOrWhiteSpace(hostDir))
                {
                    // Graceful default if not provided
                    hostDir = Path.Combine(Directory.GetCurrentDirectory(), "selenoid-downloads");
                }
                downloadDir = new DirectoryInfo(hostDir);
            }
            else if (isRemote)
            {
                // Remote Grid managed-downloads: keep the per-thread local subdir you had
                string threadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
                downloadDir = new DirectoryInfo(
                    Path.Combine(Directory.GetCurrentDirectory(), "downloads", threadId));
            }
            else
            {
                // Local runs
                downloadDir = new DirectoryInfo(
                    Path.Combine(Directory.GetCurrentDirectory(), "downloads"));
            }
        }

        /// <summary>
        /// Clean local folder and, if remote+non-Selenoid, clear the Grid-managed store for this session.
        /// Call BEFORE clicking the element that triggers the download.
        /// </summary>
        public void PrepareDownloadDir()
        {
            if (isRemote && !isSelenoid && driver is RemoteWebDriver remote)
            {
                try
                {
                    remote.DeleteDownloadableFiles();
                    Console.WriteLine("[DownloadHelper] Cleared remote managed downloads.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[DownloadHelper] Warning: could not clear remote downloads: " + ex.Message);
                }
            }
            // For Selenoid (remote+isSelenoid) we do NOT call Managed Downloads APIs (unsupported).
            // We only clean the host-mounted folder locally (safe per isolated GHA workspace).

            try
            {
                if (!downloadDir.Exists) downloadDir.Create();
                foreach (var f in downloadDir.GetFiles())
                {
                    try { f.Delete(); }
                    catch (Exception e) { Console.WriteLine("[DownloadHelper] Failed to delete local file: " + e.Message); }
                }
                Console.WriteLine("[DownloadHelper] Local download dir ready: " + downloadDir.FullName);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to prepare download directory: " + downloadDir.FullName, e);
            }
        }

        /// <summary>
        /// Waits for a file to appear and stabilize, then returns it.
        /// - Remote + non-Selenoid => Managed Downloads (unchanged)
        /// - Remote + Selenoid     => Poll the mounted host folder (NEW)
        /// - Local                 => Poll local folder (unchanged)
        /// </summary>
        public FileInfo DownloadWhenReady(string? expectedServerFileName = null,
                                          Func<string, bool>? serverNamePredicate = null)
        {
            int waitTimeSeconds = int.Parse(ConfigLoader.Get("wait.timeout.seconds") ?? "45");
            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + waitTimeSeconds * 1000L;

            Console.WriteLine($"[DownloadHelper] Waiting up to {waitTimeSeconds}s (remote={isRemote}, selenoid={isSelenoid})");

            if (isRemote && !isSelenoid && driver is RemoteWebDriver remote)
            {
                // === Remote Grid (non-Selenoid) — Managed Downloads path (your current behavior) ===
                var downloadedOnce = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string? lastChosen = null;
                long previousSize = -1;

                while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < end)
                {
                    try
                    {
                        var all = remote.GetDownloadableFiles();
                        Console.WriteLine($"[DownloadHelper] Remote file list: {string.Join(", ", all)}");

                        string? serverName = null;

                        if (!string.IsNullOrWhiteSpace(expectedServerFileName))
                        {
                            serverName = all.FirstOrDefault(n =>
                                n.Equals(expectedServerFileName, StringComparison.OrdinalIgnoreCase));
                            if (serverName == null)
                            {
                                System.Threading.Thread.Sleep(250);
                                continue;
                            }
                        }
                        else if (serverNamePredicate != null)
                        {
                            serverName = all.FirstOrDefault(n =>
                                !IsPartial(n) && !IsDotfile(n) && serverNamePredicate(n));
                        }
                        else
                        {
                            serverName = all
                                .Where(n => !IsPartial(n) && !IsDotfile(n))
                                .OrderBy(n => n)
                                .LastOrDefault();
                        }

                        if (serverName == null)
                        {
                            System.Threading.Thread.Sleep(250);
                            continue;
                        }

                        var localPath = Path.Combine(downloadDir.FullName, serverName);
                        var local = new FileInfo(localPath);

                        if (!downloadedOnce.Contains(serverName))
                        {
                            Console.WriteLine($"[DownloadHelper] Downloading '{serverName}' -> {localPath}");
                            try
                            {
                                remote.DownloadFile(serverName, downloadDir.FullName);
                                downloadedOnce.Add(serverName);
                            }
                            catch (IOException ioex) when (ioex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine("[DownloadHelper] Local file already exists; continuing to stability check.");
                                downloadedOnce.Add(serverName);
                            }

                            System.Threading.Thread.Sleep(150);
                            local.Refresh();
                        }
                        else
                        {
                            local.Refresh();
                        }

                        if (!local.Exists)
                        {
                            Console.WriteLine("[DownloadHelper] Local file still not visible; retrying...");
                            System.Threading.Thread.Sleep(200);
                            continue;
                        }

                        long size = local.Length;
                        Console.WriteLine($"[DownloadHelper] {local.Name} size={size} (prev={previousSize})");

                        if (lastChosen != null &&
                            lastChosen.Equals(serverName, StringComparison.OrdinalIgnoreCase) &&
                            size > 0 && size == previousSize)
                        {
                            Console.WriteLine($"[DownloadHelper] {local.Name} is stable. Returning.");
                            return local;
                        }

                        lastChosen = serverName;
                        previousSize = size;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[DownloadHelper] Poll exception: " + e);
                    }

                    System.Threading.Thread.Sleep(250);
                }

                throw new Exception("File was not fully downloaded and stable within timeout (remote-managed).");
            }

            // === Selenoid (remote+isSelenoid) OR Local ===
            // We simply poll the local folder (Selenoid case: it is the host-mounted download dir)
            {
                long previousSize = -1;
                string? lastName = null;

                while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < end)
                {
                    foreach (var f in downloadDir.EnumerateFiles())
                    {
                        if (f.Name.EndsWith(".crdownload", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.IsNullOrWhiteSpace(expectedServerFileName) &&
                            !f.Name.Equals(expectedServerFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (serverNamePredicate != null && !serverNamePredicate(f.Name))
                            continue;

                        f.Refresh();
                        long size = f.Length;
                        Console.WriteLine($"[DownloadHelper] Local {f.Name} size={size} (prev={previousSize})");

                        if (lastName != null &&
                            f.Name.Equals(lastName, StringComparison.OrdinalIgnoreCase) &&
                            size > 0 && size == previousSize)
                        {
                            Console.WriteLine($"[DownloadHelper] Local {f.Name} is stable. Returning.");
                            return f;
                        }

                        lastName = f.Name;
                        previousSize = size;
                    }

                    System.Threading.Thread.Sleep(250);
                }

                throw new Exception(isRemote && isSelenoid
                    ? "File was not fully downloaded and stable within timeout (selenoid-mounted)."
                    : "File was not fully downloaded and stable within timeout (local).");
            }
        }

        private static bool IsPartial(string name) =>
            name.EndsWith(".crdownload", StringComparison.OrdinalIgnoreCase);

        private static bool IsDotfile(string name) =>
            name.StartsWith(".", StringComparison.Ordinal); // e.g., .com.google.Chrome.*
    }
}
