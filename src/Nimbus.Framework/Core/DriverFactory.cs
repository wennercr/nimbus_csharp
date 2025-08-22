using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using Nimbus.Framework.Utils;

namespace Nimbus.Framework.Core
{
    public class DriverFactory
    {
        private readonly string browser;
        private readonly bool remote;
        private readonly bool headless;
        private readonly bool useProxy;
        private readonly string proxyAddress;
        private readonly string testSuiteName;
        private readonly string gridUrl;
        private readonly bool useDriverExe;
        private readonly string driverPath;

        /// <summary>
        /// Constructs a DriverFactory instance using values loaded via ConfigLoader.
        /// These values are typically passed via CLI or CI environment variables.
        /// </summary>
        public DriverFactory()
        {
            this.browser = ConfigLoader.Get("browser") ?? "chrome";
            this.remote = bool.TryParse(ConfigLoader.Get("remote"), out var r) && r;
            this.headless = bool.TryParse(ConfigLoader.Get("headless"), out var h) && h;
            this.useProxy = bool.TryParse(ConfigLoader.Get("useProxy"), out var p) && p;
            this.proxyAddress = ConfigLoader.Get("proxyAddress") ?? "";
            this.testSuiteName = ConfigLoader.Get("testSuiteName") ?? "SampleSuite";
            this.gridUrl = ConfigLoader.Get("gridUrl") ?? "http://localhost:4444/";
            this.useDriverExe = bool.TryParse(ConfigLoader.Get("useDriverExe"), out var d) && d;
            this.driverPath = ConfigLoader.Get("driverPath") ?? "./drivers";
            this.driverPath = ResolveDriversDir();
        }

        /// <summary>
        /// Creates and returns a fully configured IWebDriver instance. The browser type, execution
        /// mode, and all options are based on current configuration.
        /// </summary>
        public IWebDriver CreateDriver()
        {
            IWebDriver driver;

            switch (browser)
            {
                case "firefox":
                    {
                        var firefoxOptions = GetFirefoxOptions();
                        driver = remote ? CreateRemoteDriver(firefoxOptions) : CreateLocalFirefoxDriver(firefoxOptions);
                        break;
                    }
                case "edge":
                    {
                        var edgeOptions = GetEdgeOptions();
                        driver = remote ? CreateRemoteDriver(edgeOptions) : CreateLocalEdgeDriver(edgeOptions);
                        break;
                    }
                case "chrome":
                default:
                    {
                        var chromeOptions = GetChromeOptions();
                        driver = remote ? CreateRemoteDriver(chromeOptions) : CreateLocalChromeDriver(chromeOptions);
                        break;
                    }
            }

            AdjustWindowSize(driver);
            return driver;
        }

        /// <summary>Builds and returns Chrome-specific options with common config applied.</summary>
        private ChromeOptions GetChromeOptions()
        {
            var options = new ChromeOptions();
            ConfigureOptions(options);
            return options;
        }

        /// <summary>Builds and returns Firefox-specific options with common config applied.</summary>
        private FirefoxOptions GetFirefoxOptions()
        {
            var options = new FirefoxOptions();
            ConfigureOptions(options);
            return options;
        }

        /// <summary>Builds and returns Edge-specific options with common config applied.</summary>
        private EdgeOptions GetEdgeOptions()
        {
            var options = new EdgeOptions();
            ConfigureOptions(options);
            return options;
        }

        /// <summary>
        /// Applies standard configuration to browser options. This includes headless mode, proxy,
        /// download settings, and remote capabilities.
        /// </summary>
        private void ConfigureOptions(DriverOptions options)
        {
            AddDefaultArguments(options);
            ApplyHeadlessMode(options);
            AddCommonProxy(options);
            ConfigureDownloadDirectory(options);

            if (remote)
            {
                AddRemoteCapabilities(options);
            }
        }

        /// <summary>
        /// Adds general-purpose arguments used across all browser types.
        /// </summary>
        private void AddDefaultArguments(DriverOptions options)
        {
            var args = new[]
            {
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--ignore-certificate-errors",
                "--disable-gpu",
                "--disable-software-rasterizer",
                "--safebrowsing-disable-download-protection"
            };

            switch (options)
            {
                case ChromeOptions chrome:
                    chrome.AddArguments(args);
                    break;
                case FirefoxOptions firefox:
                    foreach (var a in args) firefox.AddArgument(a);
                    break;
                case EdgeOptions edge:
                    foreach (var a in args) edge.AddArgument(a);
                    break;
            }
        }

        /// <summary>
        /// Applies headless mode to the given browser options if configured.
        /// </summary>
        private void ApplyHeadlessMode(DriverOptions options)
        {
            if (!headless) return;

            switch (options)
            {
                case FirefoxOptions ff:
                    ff.AddArgument("-headless");
                    break;
                case ChromeOptions ch:
                    ch.AddArgument("--headless=new");
                    break;
                case EdgeOptions ed:
                    ed.AddArgument("--headless=new");
                    break;
            }
        }

        /// <summary>
        /// Applies proxy settings to the given browser options if enabled.
        /// </summary>
        private void AddCommonProxy(DriverOptions options)
        {
            if (!useProxy) return;
            Console.WriteLine("I am about to execute proxy logic");

            var proxy = new Proxy
            {
                HttpProxy = proxyAddress,
                SslProxy = proxyAddress
            };
            options.Proxy = proxy;

            var proxyArg = $"--proxy-server={proxyAddress}";
            switch (options)
            {
                case ChromeOptions ch:
                    ch.AddArgument(proxyArg);
                    break;
                case FirefoxOptions ff:
                    ff.AddArgument(proxyArg);
                    break;
                case EdgeOptions ed:
                    ed.AddArgument(proxyArg);
                    break;
            }
        }

        /// <summary>
        /// Configures download directory and behavior for the given browser options.
        /// </summary>
        private void ConfigureDownloadDirectory(DriverOptions options)
        {
            string containerDownloadDir;
            if (bool.TryParse(ConfigLoader.Get("remote"), out var isRemote) && isRemote)
            {
                containerDownloadDir = Environment.GetEnvironmentVariable("DOWNLOAD_DIR") ?? "";
            }
            else
            {
                containerDownloadDir = Path.Combine(Directory.GetCurrentDirectory(), "downloads");
                Console.WriteLine($"download path = {containerDownloadDir}");
            }

            switch (options)
            {
                case FirefoxOptions firefox:
                    firefox.SetPreference("browser.download.folderList", 2);
                    firefox.SetPreference("browser.download.dir", containerDownloadDir);
                    firefox.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/pdf");
                    firefox.SetPreference("pdfjs.disabled", true);
                    break;

                case ChromeOptions chrome:
                    {
                        chrome.AddUserProfilePreference("download.default_directory", containerDownloadDir);
                        chrome.AddUserProfilePreference("download.prompt_for_download", false);
                        chrome.AddUserProfilePreference("download.open_pdf_in_system_reader", false);
                        chrome.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
                        chrome.AddUserProfilePreference("credentials_enable_service", false);
                        chrome.AddUserProfilePreference("profile.password_manager_enabled", false);
                        chrome.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
                        break;
                    }

                case EdgeOptions edge:
                    edge.AddUserProfilePreference("download.default_directory", containerDownloadDir);
                    edge.AddUserProfilePreference("download.prompt_for_download", false);
                    edge.AddUserProfilePreference("download.open_pdf_in_system_reader", false);
                    // Same key works for Edge (Chromium-based):
                    edge.AddUserProfilePreference("plugins.always_open_pdf_externally", true);
                    edge.AddUserProfilePreference("credentials_enable_service", false);
                    edge.AddUserProfilePreference("profile.password_manager_enabled", false);
                    edge.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
                    break;
            }
        }

        /// <summary>
        /// Adds Selenium Grid-specific capabilities (test name, downloads enabled, platform, logging).
        /// </summary>
        private void AddRemoteCapabilities(DriverOptions options)
        {
            options.AddAdditionalOption("se:name", testSuiteName);
            options.AddAdditionalOption("se:downloadsEnabled", true);
            options.PlatformName = "linux";

            // Logging prefs (Chromium)
            var loggingPrefs = new Dictionary<string, object> { ["browser"] = "ALL" };
            options.AddAdditionalOption("goog:loggingPrefs", loggingPrefs);
        }

        /// <summary>
        /// Creates and returns a RemoteWebDriver using the provided options and Grid URL.
        /// </summary>
        private RemoteWebDriver CreateRemoteDriver(DriverOptions options)
        {
            try
            {
                Console.WriteLine($"Remote Grid URL being used: {gridUrl}");
                var uri = new Uri(gridUrl);
                return new RemoteWebDriver(uri, options);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to create RemoteWebDriver: {e.Message}");
                Console.Error.WriteLine(e);
                throw new Exception($"Error creating RemoteWebDriver with Grid URL: {gridUrl}", e);
            }
        }

        private IWebDriver CreateLocalChromeDriver(ChromeOptions options)
        {
            if (useDriverExe)
            {
                var exePath = Path.Combine(driverPath ?? string.Empty, "chromedriver.exe");
                var service = ChromeDriverService.CreateDefaultService(
                    Path.GetDirectoryName(exePath) ?? driverPath,
                    Path.GetFileName(exePath));
                return new ChromeDriver(service, options);
            }
            return new ChromeDriver(options);
        }

        private IWebDriver CreateLocalFirefoxDriver(FirefoxOptions options)
        {
            if (useDriverExe)
            {
                var exePath = Path.Combine(driverPath ?? string.Empty, "geckodriver.exe");
                var service = FirefoxDriverService.CreateDefaultService(
                    Path.GetDirectoryName(exePath) ?? driverPath,
                    Path.GetFileName(exePath));
                return new FirefoxDriver(service, options);
            }
            return new FirefoxDriver(options);
        }

        private IWebDriver CreateLocalEdgeDriver(EdgeOptions options)
        {
            if (useDriverExe)
            {
                var exePath = Path.Combine(driverPath ?? string.Empty, "msedgedriver.exe");
                var service = EdgeDriverService.CreateDefaultService(
                    Path.GetDirectoryName(exePath) ?? driverPath,
                    Path.GetFileName(exePath));
                return new EdgeDriver(service, options);
            }
            return new EdgeDriver(options);
        }

        private static string ResolveDriversDir()
        {
            // Start from the folder where tests execute, then walk up to solution root
            var baseDir = AppContext.BaseDirectory;
            var dir = new DirectoryInfo(baseDir);

            // Walk up a few levels looking for a "drivers" folder at solution level
            for (int i = 0; i < 6 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "drivers");
                if (Directory.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
            "Could not locate repository root. Add a root marker like .git/, a *.sln file, drivers/, or .nimbusroot."
            );
        }

        /// <summary>
        /// Adjusts the browser window size based on execution mode:
        /// - Maximizes window for local, headed runs
        /// - Sets fixed 1920x1080 size for headless or remote runs
        /// </summary>
        private void AdjustWindowSize(IWebDriver driver)
        {
            if (!remote && !headless)
            {
                driver.Manage().Window.Maximize();
            }
            else
            {
                driver.Manage().Window.Size = new System.Drawing.Size(1920, 1080);
            }
        }
    }
}
