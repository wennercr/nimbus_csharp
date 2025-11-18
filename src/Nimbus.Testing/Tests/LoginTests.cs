using Nimbus.Testing.Pages;
using NUnit.Framework;
using Allure.Net.Commons;
using Nimbus.Testing.Base;
using Nimbus.Framework.Utils;
using Allure.NUnit.Attributes;

namespace Nimbus.Testing.Tests
{
    /// <summary>
    /// Example test class for validating login functionality using the Nimbus framework.
    ///
    /// This class extends <see cref="TestBase"/>, which provides full test lifecycle management:
    /// - Thread-safe WebDriver setup and teardown
    /// - Screenshot capture on failure
    /// - Allure reporting integration
    /// - Parallel test support
    /// </summary>
    [TestFixture]
    [Category("Smoke")]
    public class LoginTest : TestBase
    {
        /// <summary>
        /// Runs before every test method to navigate to the login page.
        /// </summary>
        [SetUp]
        public void NavigateToLoginPage()
        {
            Driver.Navigate().GoToUrl(ConfigLoader.Get("base.url") ?? "https://demo.guru99.com/test/newtours/");
        }

        /// <summary>
        /// Verifies that a user can log in successfully with valid credentials.
        /// </summary>
        [Test]
        [Category("regression")]
        [Category("Smoke")]
        [Category("login")]
        [AllureTag("login")]
        [AllureSeverity(SeverityLevel.critical)]
        [AllureDescription("Checks that a user can log in using correct email and password.")]
        [Description("Verify user login with valid credentials")]
        public void ValidLoginShouldSucceed()
        {
            var loginPage = new LoginPage(Driver);
            loginPage.Login("tutorial", "tutorial");




            // Assert.That(
            //     Driver.PageSource,
            //     Does.Contain("test me out"),
            //     "Login was not successful.");
        }

        /// <summary>
        /// Verifies that an incorrect password triggers a visible error message on the login page.
        /// </summary>
        [Test]
        [Category("login")]
        public void InvalidLoginShouldDisplayError()
        {
            var loginPage = new LoginPage(Driver);
            loginPage.Login("tutorial", "incorrectPassword");
            loginPage.VerifyLoginErrorMessage();
            Assert.Fail("failing on purpose for screenshot");
        }

        /// <summary>
        /// Parallel test case for concurrency validation.
        /// </summary>
        [Test]
        [Category("Smoke")]
        [Category("login")]
        public void ParallelTest()
        {
            var loginPage = new LoginPage(Driver);
            loginPage.Login("tutorial", "incorrectPassword");
            loginPage.VerifyLoginErrorMessage();
        }

        [Test]
        [Category("login")]
        public void ParallelTest1()
        {
            var loginPage = new LoginPage(Driver);
            loginPage.Login("tutorial", "incorrectPassword");
            loginPage.VerifyLoginErrorMessage();
        }

        [Test]
        [Category("login")]
        public void ParallelTest2()
        {
            var loginPage = new LoginPage(Driver);
            loginPage.Login("tutorial", "incorrectPassword");
            loginPage.VerifyLoginErrorMessage();
        }

        [Test]
        [Category("login")]
        public void ParallelTest3()
        {
            var loginPage = new LoginPage(Driver);
            loginPage.Login("tutorial", "incorrectPassword");
            loginPage.VerifyLoginErrorMessage();
        }
    }
}
