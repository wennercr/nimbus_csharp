// File: src/Nimbus.Framework/Utils/Logger/AllureHelper.cs
using System.Text;
using Allure.Net.Commons;
using OpenQA.Selenium;

namespace Nimbus.Framework.Utils.Logger
{
    public static class AllureHelper
    {
        /// <summary>
        /// Creates a REAL Allure step (and mirrors to console),
        /// then also attaches the message as a small text file (parity with Java helper).
        /// </summary>
        public static void LogStep(string message)
        {
            Console.WriteLine(message ?? string.Empty);
            // No-op step (shows up like a log line in Allure)
            AllureApi.Step(message ?? string.Empty);
        }

        /// <summary>
        /// Same as LogStep but lets you wrap executable code in the step.
        /// </summary>
        public static void Step(string name, Action action)
        {
            AllureApi.Step(string.IsNullOrWhiteSpace(name) ? "Step" : name, action);
        }

        public static async Task StepAsync(string name, Func<Task> action)
        {
            await AllureApi.Step(string.IsNullOrWhiteSpace(name) ? "Step" : name, action);
        }

        public static void AttachText(string name, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
            TryAllureAttachBytes(name, bytes, "text/plain", "txt");
        }

        public static void AttachScreenshot(IWebDriver driver, string name = "screenshot.png")
        {
            try
            {
                if (driver is ITakesScreenshot ss)
                {
                    var bytes = ss.GetScreenshot()?.AsByteArray;
                    TryAllureAttachBytes(
                        string.IsNullOrWhiteSpace(name) ? "screenshot.png" : name,
                        bytes, "image/png", "png");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nimbus] Screenshot attach failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Core attach: writes bytes to allure-results and links to the TEST level.
        /// </summary>
        public static void TryAllureAttachBytes(string name, byte[] data, string mime, string fileExt)
        {
            if (data == null || data.Length == 0) return;

            try
            {
                // New, simpler API that handles writing + linking
                AllureApi.AddAttachment(
                    string.IsNullOrWhiteSpace(name) ? $"attachment.{fileExt}" : name,
                    mime,
                    data,
                    fileExt ?? string.Empty
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nimbus] Attachment failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Attach under the CURRENT STEP (call inside AllureApi.Step body if you want nesting).
        /// </summary>
        public static void TryAllureAttachBytesToStep(string name, byte[] data, string mime, string fileExt)
        {
            if (data == null || data.Length == 0) return;

            try
            {
                // When called inside a Step(...) body, AddAttachment will link to the current step.
                AllureApi.AddAttachment(
                    string.IsNullOrWhiteSpace(name) ? $"attachment.{fileExt}" : name,
                    mime,
                    data,
                    fileExt ?? string.Empty
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nimbus] Step attachment failed: {ex.Message}");
            }
        }
    }
}
