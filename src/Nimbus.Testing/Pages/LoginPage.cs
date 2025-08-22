using OpenQA.Selenium;
using NUnit.Framework;                 // only for the two assertion helpers you already expose on the page
using Nimbus.Framework.Core;           // PageObjectBase
using Nimbus.Framework.Utils.Logger;   // AllureHelper

namespace Nimbus.Testing.Pages
{
    /// <summary>
    /// POM for the Guru99 Newtours login page, implemented WITHOUT PageFactory.
    /// - Elements are defined as By locators (no cached IWebElement fields).
    /// - All interactions go through PageObjectBase helpers (which re-find elements at action time).
    /// - Method names match your existing API to minimize downstream changes.
    /// </summary>
    public class LoginPage : PageObjectBase
    {
        // ====================
        // Locators (By-based)
        // ====================

        // Using the same selectors you had on the FindsBy attributes
        private static readonly By UsernameField = By.Name("userName");
        private static readonly By PasswordField = By.Name("password");
        private static readonly By LoginButton = By.Name("submit");
        private static readonly By LoginErrorMessage = By.XPath("//span[contains(text(),'Enter your userName and password correct')]");

        /// <summary>
        /// Standard ctor uses default wait timeout from config (via PageObjectBase).
        /// </summary>
        public LoginPage(IWebDriver driver) : base(driver) { }

        /// <summary>
        /// Alternate ctor lets a specific timeout be provided for this page.
        /// </summary>
        public LoginPage(IWebDriver driver, int timeoutInSeconds) : base(driver, timeoutInSeconds) { }

        // ====================
        // Page Actions
        // ====================

        /// <summary>
        /// Navigate to the login page URL and wait for the username input to be visible.
        /// Keeps navigation logic local to the page.
        /// </summary>
        public LoginPage GoTo(string url)
        {
            AllureHelper.LogStep($"[NAVIGATE] Opening login page: {url}");
            driver.Navigate().GoToUrl(url);
            // Ensure the page is ready enough to interact (username should be visible)
            WaitForVisibility(UsernameField);
            return this;
        }

        /// <summary>
        /// Attempts to log in by entering the username and password then clicking the login button.
        /// Internally uses By-based helpers (fresh element every time, fewer stale element failures).
        /// </summary>
        public void Login(string username, string password)
        {
            AllureHelper.LogStep($"[ACTION] Login with username: '{username}' | password: '(hidden)'");
            // PageObjectBase.SendKeys/Click already include explicit waits + logging
            SendKeys(UsernameField, username);
            SendKeys(PasswordField, password);
            Click(LoginButton);
        }

        /// <summary>
        /// Verifies that the known login error message is visible.
        /// Kept as an assertion here to preserve your original page API; alternatively,
        /// you can expose a bool IsLoginErrorVisible() and assert in the test layer.
        /// </summary>
        public void VerifyLoginErrorMessage()
        {
            AllureHelper.LogStep("[VERIFY] Expecting login error banner to be displayed.");
            // Visible(...) waits until displayed or times out
            var error = Visible(LoginErrorMessage);
            Assert.That(error.Displayed, "The error message was not displayed after incorrect login credentials were entered");
        }

        /// <summary>
        /// Intentionally fails by asserting the error message is NOT visible.
        /// Useful to validate failure screenshots and reporting.
        /// </summary>
        public void FailOnPurpose()
        {
            AllureHelper.LogStep("[VERIFY] Intentionally expecting NO error banner (this should fail).");
            // Exists(...) is a fast presence check (no wait). If you want to be stricter, use Visible(LoginErrorMessage) with try/catch.
            var exists = Exists(LoginErrorMessage);
            Assert.That(exists, Is.False, "We are failing this on purpose.");
        }

        // ====================
        // Optional ergonomic helpers (if you want them)
        // ====================

        /// <summary>
        /// Returns true if the login error is present (no waiting). Handy for conditional flows.
        /// </summary>
        public bool IsLoginErrorPresent() => Exists(LoginErrorMessage);

        /// <summary>
        /// Returns the text of the login error if visible; throws on timeout otherwise.
        /// </summary>
        public string ReadLoginErrorText() => GetText(LoginErrorMessage);
    }
}
