using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Nimbus.Framework.Utils
{
    /// <summary>
    /// Generic download helper that supports BOTH Remote (Selenium Grid Managed Downloads)
    /// and Local runs. It can:
    ///  - Start with a clean state (local dir + remote managed store)
    ///  - Poll for a completed file on the server (no ".crdownload" or dotfiles)
    ///  - Download each server file only once
    ///  - Verify local file "size stability" before returning (works for any file type)
    /// </summary>
    public class DownloadHelper
    {
        private readonly IWebDriver driver;
        private readonly DirectoryInfo downloadDir;
        private readonly bool isRemote;

        /// <summary>
        /// Constructs a per-thread download directory to avoid collisions.
        /// Mirrors the Java behavior: remote flag pulled from ConfigLoader for parity.
        /// </summary>
        public DownloadHelper(IWebDriver driver)
        {
            this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
            this.isRemote = bool.TryParse(ConfigLoader.Get("remote"), out var r) && r;

            if (this.isRemote)
            {
                string threadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
                this.downloadDir = new DirectoryInfo(
                    Path.Combine(Directory.GetCurrentDirectory(), "downloads", threadId));
            }
            else
            {
                this.downloadDir = new DirectoryInfo(
                    Path.Combine(Directory.GetCurrentDirectory(), "downloads"));
            }
        }

        /// <summary>
        /// Clean local folder and, if remote, clear the Grid-managed store for this session.
        /// Call this BEFORE clicking the element that triggers the download.
        /// </summary>
        public void PrepareDownloadDir()
        {
            if (isRemote && driver is RemoteWebDriver remote)
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
        /// Works for any file type.
        /// 
        /// You can optionally target a specific server-side name or provide a predicate to choose a file.
        /// If both are null, the helper picks the latest completed non-dot, non-.crdownload entry.
        /// </summary>
        /// <param name="expectedServerFileName">
        /// Exact server-side name to wait for (e.g. "drylab.pdf"). Optional.
        /// </param>
        /// <param name="serverNamePredicate">
        /// Predicate to select a server file (e.g. n => n.EndsWith(".zip", OrdinalIgnoreCase)). Optional.
        /// </param>
        /// <returns>Stable local FileInfo</returns>
        public FileInfo DownloadWhenReady(string? expectedServerFileName = null,
                                          Func<string, bool>? serverNamePredicate = null)
        {
            int waitTimeSeconds = int.Parse(ConfigLoader.Get("wait.timeout.seconds") ?? "45");
            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + waitTimeSeconds * 1000L;

            Console.WriteLine($"[DownloadHelper] Waiting up to {waitTimeSeconds}s (remote={isRemote})");

            if (isRemote && driver is RemoteWebDriver remote)
            {
                // Track which server files we've already transferred locally to avoid re-download attempts.
                var downloadedOnce = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string? lastChosen = null;
                long previousSize = -1;

                while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < end)
                {
                    try
                    {
                        var all = remote.GetDownloadableFiles();
                        Console.WriteLine($"[DownloadHelper] Remote file list: {string.Join(", ", all)}");

                        // Choose a server file:
                        string? serverName = null;

                        if (!string.IsNullOrWhiteSpace(expectedServerFileName))
                        {
                            serverName = all.FirstOrDefault(n =>
                                n.Equals(expectedServerFileName, StringComparison.OrdinalIgnoreCase));
                            if (serverName == null)
                            {
                                // Still waiting for the specific file to show
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
                            // Either only partials/dotfiles exist, or the expected file hasn't appeared yet
                            System.Threading.Thread.Sleep(250);
                            continue;
                        }

                        var localPath = Path.Combine(downloadDir.FullName, serverName);
                        var local = new FileInfo(localPath);

                        // Download only once per server name
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
                                // A previous poll already pulled it — mark as downloaded and move on to stability check
                                Console.WriteLine("[DownloadHelper] Local file already exists; continuing to stability check.");
                                downloadedOnce.Add(serverName);
                            }

                            // small flush wait
                            System.Threading.Thread.Sleep(150);
                            local.Refresh();
                        }
                        else
                        {
                            // Already downloaded; just re-check stability
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

                        // "Stable twice" rule: same size in two consecutive polls and non-zero
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

                throw new Exception("File was not fully downloaded and stable within timeout (remote).");
            }
            else
            {
                // === LOCAL MODE ===
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
                            continue; // user asked for a specific name
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

                throw new Exception("File was not fully downloaded and stable within timeout (local).");
            }
        }

        private static bool IsPartial(string name) =>
            name.EndsWith(".crdownload", StringComparison.OrdinalIgnoreCase);

        private static bool IsDotfile(string name) =>
            name.StartsWith(".", StringComparison.Ordinal); // e.g., .com.google.Chrome.*
    }
}
