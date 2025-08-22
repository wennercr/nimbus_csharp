using NUnit.Framework;
using OpenQA.Selenium;
using Nimbus.Framework.Core;          // PageObjectBase
using Nimbus.Framework.Utils.Logger;  // AllureHelper (optional logging)
using Nimbus.Framework.Utils;

namespace Nimbus.Testing.Pages
{
    /// <summary>
    /// Page Object for the File-Examples PDF download page implemented WITHOUT PageFactory.
    /// - Locators are By-based (no cached IWebElement fields).
    /// - Interactions go through PageObjectBase helpers (which re-find elements at action time).
    /// - Keeps your existing method name: ClickAndVerifyDownloadContents().
    /// </summary>
    public class FileExamplesPdfPage : PageObjectBase
    {
        // ====================
        // Locators (By-based)
        // ====================
        // These mirror your original [FindsBy] XPath selectors.
        // Keeping them private promotes encapsulation (page methods drive interactions).
        private static readonly By PageHeader = By.XPath("//h1[text()='Sample Documents']");
        private static readonly By NewsletterPdfLink = By.XPath("//div[@id='newsletter']//a[text()='PDF']");

        /// <summary>
        /// Standard ctor uses default wait timeout from config (via PageObjectBase).
        /// </summary>
        public FileExamplesPdfPage(IWebDriver driver) : base(driver) { }

        /// <summary>
        /// Clicks the newsletter PDF link, waits for the download, reads the PDF, and asserts on its content.
        /// This preserves your original method name/signature for downstream compatibility.
        /// </summary>
        /// <remarks>
        /// Steps:
        ///  1) Verify page is loaded (header visible)
        ///  2) Prepare download directory (local or Grid-managed, handled by DownloadHelper)
        ///  3) Click the PDF link
        ///  4) Wait for the file to appear
        ///  5) Read PDF text and assert expected content
        /// </remarks>
        public void ClickAndVerifyDownloadContents()
        {
            // 1) Confirm page is loaded
            AllureHelper.LogStep("[VERIFY] Confirming 'Sample Documents' header is visible.");
            WaitForVisibility(PageHeader); // throws on timeout

            // 2) Prepare download directory
            var downloadHelper = new DownloadHelper(driver);
            downloadHelper.PrepareDownloadDir(); // handles local dir cleanup or Grid download setup

            // 3) Trigger the download
            AllureHelper.LogStep("[ACTION] Clicking Newsletter PDF link to start download.");
            Click(NewsletterPdfLink); // PageObjectBase.Click waits for clickability

            // 4) Wait for the file to be ready
            AllureHelper.LogStep("[WAIT] Waiting for downloaded PDF file to appear.");
            FileInfo downloadedFile = downloadHelper.DownloadWhenReady();

            // 5) Read and assert PDF content
            var pdfHelper = new PdfHelper();
            string pdfContent = pdfHelper.Read(downloadedFile);

            AllureHelper.LogStep("[ASSERT] Verifying expected text is present in the PDF.");
            Assert.That(pdfContent, Does.Contain("Welcome to our first newsletter of 2017"),
                "Expected newsletter welcome text was not found in the downloaded PDF.");
        }
    }
}
