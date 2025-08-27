using Nimbus.Testing.Pages;
using Nimbus.Testing.Base;
using NUnit.Framework;
using Allure.NUnit.Attributes;

namespace Nimbus.Testing.Tests
{
    /// <summary>
    /// Test class that validates PDF download and content from a live sample site.
    ///
    /// This test demonstrates:
    /// - Downloading a file through a browser running in Selenium Grid
    /// - Reading the PDF file using a custom utility
    /// - Validating that the PDF contains specific expected content
    ///
    /// The test extends <see cref="TestBase"/>, which provides:
    /// - Thread-safe WebDriver setup
    /// - Parallel test support
    /// - Built-in reporting via Allure
    /// - Screenshot-on-failure logic
    /// </summary>
    [TestFixture]
    public class FileExamplesPdfTest : TestBase
    {
        /// <summary>
        /// Executed before each test method to load the PDF download source page.
        /// </summary>
        [SetUp]
        public void NavigateToDownloadPage()
        {
            Driver.Navigate().GoToUrl("https://www.princexml.com/samples/");
        }

        /// <summary>
        /// Full end-to-end test that downloads the PDF and validates its contents.
        /// </summary>
        [Test]
        [Description("Verify downloaded PDF from file-examples.com contains expected text")]
        public void VerifyDownloadedPdfFromFileExamples()
        {
            var page = new FileExamplesPdfPage(Driver);
            page.ClickAndVerifyDownloadContents();
        }
    }
}
