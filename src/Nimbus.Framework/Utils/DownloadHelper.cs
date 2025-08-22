
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Nimbus.Framework.Utils
{
    /// <summary>
    /// Utility class for retrieving files downloaded during test execution.
    ///
    /// <para>
    /// Supports both <b>remote Selenium Grid sessions</b> (using the Managed Downloads API)
    /// and <b>local WebDriver sessions</b> (by polling the local download directory).
    /// </para>
    ///
    /// <para>For Remote mode this requires:</para>
    /// <list type="bullet">
    ///   <item>Selenium Grid 4.x+ started with <c>--enable-managed-downloads</c></item>
    ///   <item>The <c>se:downloadsEnabled</c> capability set to <c>true</c></item>
    /// </list>
    ///
    /// <para>
    /// Typical usage:
    /// <code>
    /// var helper = new DownloadHelper(driver);
    /// helper.PrepareDownloadDir();
    /// FileInfo file = helper.DownloadWhenReady();
    /// </code>
    /// </para>
    /// </summary>
    public class DownloadHelper
    {
        private readonly IWebDriver driver;
        private readonly DirectoryInfo downloadDir;
        private readonly bool isRemote;

        /// <summary>
        /// Constructs a thread-safe download helper for the current test thread.
        /// Automatically assigns a unique subdirectory under /downloads to avoid conflicts.
        /// </summary>
        /// <param name="driver">The active WebDriver from the current test session</param>
        public DownloadHelper(IWebDriver driver)
        {
            this.driver = driver;
            this.isRemote = bool.Parse(ConfigLoader.Get("remote") ?? "true");

            // Create a per-thread folder (downloads/<threadName>) to prevent parallel test collisions
            if (this.isRemote)
            {
                string threadId = System.Threading.Thread.CurrentThread.Name!
                    .Replace("[^a-zA-Z0-9]", "_");
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
        /// Ensure download directory exists and is clean BEFORE download starts.
        /// </summary>
        public void PrepareDownloadDir()
        {
            try
            {
                if (downloadDir.Exists)
                {
                    foreach (var file in downloadDir.GetFiles())
                    {
                        try { file.Delete(); }
                        catch (Exception)
                        {
                            Console.Error.WriteLine("Failed to delete file: " + file.FullName);
                        }
                    }
                }
                else
                {
                    downloadDir.Create();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to prepare download directory: " + downloadDir.FullName, e);
            }
        }

        /// <summary>
        /// Waits for a file to be downloaded by the browser and returns the final file.
        ///
        /// Handles both local and remote Selenium WebDriver sessions.
        /// </summary>
        /// <returns>The fully downloaded <see cref="FileInfo"/>.</returns>
        public FileInfo DownloadWhenReady()
        {
            int waitTime = int.Parse(ConfigLoader.Get("wait.timeout.seconds") ?? "30");
            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (waitTime * 1000);
            long previousSize = -1;

            while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < end)
            {
                try
                {
                    if (isRemote)
                    {
                        // REMOTE MODE (Selenium Grid)
                        var remote = (RemoteWebDriver)driver;
                        IReadOnlyCollection<string> allFiles = remote.GetDownloadableFiles();
                        if (allFiles.Count == 0)
                        {
                            System.Threading.Thread.Sleep(300);
                            continue;
                        }

                        Console.WriteLine("Remote file candidate: " + allFiles.First());

                        var fileName = allFiles.FirstOrDefault(f => !f.EndsWith(".crdownload"));
                        if (fileName == null)
                        {
                            System.Threading.Thread.Sleep(300);
                            continue;
                        }

                        string localPath = Path.Combine(downloadDir.FullName, fileName);
                        remote.DownloadFile(fileName, downloadDir.FullName);

                        System.Threading.Thread.Sleep(300);

                        var f = new FileInfo(localPath);
                        if (!f.Exists) continue;

                        long size = f.Length;
                        if (size > 0 && size == previousSize)
                        {
                            return f;
                        }
                        previousSize = size;
                    }
                    else
                    {
                        // LOCAL MODE
                        foreach (var file in downloadDir.EnumerateFiles())
                        {
                            if (!file.Name.EndsWith(".crdownload"))
                            {
                                long size = file.Length;
                                if (size > 0 && size == previousSize)
                                {
                                    return file;
                                }
                                previousSize = size;
                            }
                        }
                    }
                }
                catch
                {
                    // Swallow exceptions to allow polling to continue
                }

                try
                {
                    System.Threading.Thread.Sleep(300);
                }
                catch { }
            }

            throw new Exception("File was not fully downloaded and stable within timeout.");
        }
    }
}
