using System.Text;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using Nimbus.Framework.Core;           // DriverFactory, DriverManager
using Nimbus.Framework.Utils;          // ConfigLoader
using Nimbus.Framework.Utils.Logger;
using Allure.NUnit;   // AllureHelper

namespace Nimbus.Testing.Base
{
    /// <summary>
    /// Abstract base class for all UI tests in the Nimbus framework (NUnit).
    /// Mirrors the Java TestBase responsibilities:
    /// - Per-test driver init/teardown
    /// - Allure step logging for pass/fail/disabled
    /// - Screenshot on failure
    /// - Attach runtime config to report
    /// - (Best-effort) set parallel threads from config
    /// </summary>
    [AllureNUnit]
    [SetUpFixture]
    public abstract class TestBase
    {
        /// <summary>
        /// Convenient access to the thread-local driver managed by DriverManager.
        /// </summary>
        protected IWebDriver Driver => DriverManager.Current;

        // --------------- Suite lifecycle (NUnit) ----------------

        /// <summary>
        /// Runs once per fixture (i.e., per derived test class).
        /// Java version set JUnit parallel thread count; in NUnit this is best done via runsettings or env var
        /// *before* the test run starts. We still set it here as a best effort (informational).
        /// </summary>
        [OneTimeSetUp]
        public static void BeforeAll()
        {
            TestContext.Progress.WriteLine("i am in my beforeAll");
        }

        /// <summary>
        /// Runs once per fixture after all tests complete.
        /// In Java you invoked ReportManager.generateHtmlReport(); in .NET we usually generate Allure HTML in CI.
        /// </summary>
        [OneTimeTearDown]
        public static void AfterAll()
        {
            TestContext.Progress.WriteLine("i am in my afterall");
        }

        // --------------- Test lifecycle (per test) ----------------

        /// <summary>
        /// Creates a new WebDriver before each test and stores it in DriverManager (ThreadLocal).
        /// Also attaches resolved configuration to Allure as plain text.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // Create and register a driver for this test thread
            var driver = new DriverFactory().CreateDriver();
            DriverManager.Set(driver);

            // Attach current config to Allure
            var configText = ConfigLoader.GetAllAsString() ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(configText);
            AllureHelper.TryAllureAttachBytes("Global Configuration", bytes, "text/plain", "txt");
        }

        /// <summary>
        /// Teardown after each test. 
        /// - If test failed, attach a screenshot
        /// - Always quit and remove the driver
        /// - Log result into Allure as a step (Passed / Failed / Skipped)
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            try
            {
                var context = TestContext.CurrentContext;
                var status = context.Result.Outcome.Status;
                var name = context.Test.Name;

                switch (status)
                {
                    case TestStatus.Failed:
                        {
                            var reason = context.Result.Message ?? "Test failed.";
                            var stepMsg = $"Test failed: {name}\n{reason}";
                            AllureHelper.LogStep(stepMsg);

                            TryAttachScreenshot("Failure Screenshot");
                            break;
                        }
                    case TestStatus.Passed:
                        {
                            var stepMsg = $"Test passed: {name}";
                            AllureHelper.LogStep(stepMsg);
                            break;
                        }
                    case TestStatus.Skipped:
                        {
                            var stepMsg = $"Test skipped: {name} — {context.Result.Message}";
                            AllureHelper.LogStep(stepMsg);
                            break;
                        }
                    default:
                        {
                            var stepMsg = $"Test finished with status: {status} — {name}";
                            AllureHelper.LogStep(stepMsg);
                            break;
                        }
                }
            }
            finally
            {
                // Always tear down the driver for this thread
                DriverManager.QuitAndRemove();
            }
        }

        // --------------- Helpers ----------------

        /// <summary>
        /// Attempts to capture and attach a PNG screenshot to Allure.
        /// Safe to call even if driver isn't initialized or doesn't support screenshots.
        /// </summary>
        protected void TryAttachScreenshot(string name)
        {
            try
            {
                if (!DriverManager.IsInitialized) return;

                if (Driver is ITakesScreenshot ss)
                {
                    var shot = ss.GetScreenshot();
                    var bytes = shot?.AsByteArray;
                    AllureHelper.TryAllureAttachBytes(name, bytes, "image/png", "png");
                }
            }
            catch
            {
                // Swallow — screenshots should never break teardown
            }
        }
    }
}
