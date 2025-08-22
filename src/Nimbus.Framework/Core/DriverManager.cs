using OpenQA.Selenium;

namespace Nimbus.Framework.Core
{
    /// <summary>
    /// Thread-local holder for IWebDriver with a safe "throw-if-null" access pattern.
    /// Call DriverManager.Set(driver) after creating a driver, and
    /// DriverManager.QuitAndRemove() during teardown.
    /// </summary>
    public static class DriverManager
    {
        // Nullable so we can clear it on teardown without warnings.
        private static readonly ThreadLocal<IWebDriver?> _driver = new();

        /// <summary>
        /// Gets the current thread's IWebDriver or throws if not initialized.
        /// </summary>
        public static IWebDriver Current =>
            _driver.Value ?? throw new InvalidOperationException(
                "WebDriver not initialized for this thread. Call DriverManager.Set(driver) first.");

        /// <summary>
        /// Returns true if a driver is set for this thread.
        /// </summary>
        public static bool IsInitialized => _driver.Value is not null;

        /// <summary>
        /// Sets the driver for this thread.
        /// </summary>
        public static void Set(IWebDriver driver)
        {
            if (driver is null) throw new ArgumentNullException(nameof(driver));
            _driver.Value = driver;
        }

        /// <summary>
        /// Try-get without throwing.
        /// </summary>
        public static bool TryGet(out IWebDriver? driver)
        {
            driver = _driver.Value;
            return driver is not null;
        }

        /// <summary>
        /// Quit and clear the driver for this thread.
        /// Safe to call multiple times.
        /// </summary>
        public static void QuitAndRemove()
        {
            var d = _driver.Value;
            if (d is null) return;

            try { d.Quit(); }
            catch { /* swallow teardown errors */ }
            finally
            {
                try { d.Dispose(); } catch { /* ignore */ }
                _driver.Value = null;
            }
        }

        /// <summary>
        /// Clear the driver without calling Quit (if you manage lifecycle elsewhere).
        /// </summary>
        public static void RemoveWithoutQuit()
        {
            var d = _driver.Value;
            if (d is null) return;

            try { d.Dispose(); }
            catch { /* ignore */ }
            finally { _driver.Value = null; }
        }
    }
}
