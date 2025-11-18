using Allure.NUnit.Attributes;
using Nimbus.Framework.Api;
using Nimbus.Framework.Base;
using NUnit.Framework;

namespace Nimbus.Testing.Tests
{
    /// <summary>
    /// Example API test class that demonstrates using Nimbus with the ReqRes API.
    ///
    /// This test class does not involve any UI logic, but lives in the same project to demonstrate
    /// flexibility and hybrid test setup.
    /// </summary>
    [TestFixture]
    public class CreateUserApiTest : TestBase
    {
        [SetUp]
        public void NavigateToApi()
        {
            Driver.Navigate().GoToUrl("https://reqres.in/api/users");
        }

        /// <summary>
        /// Validates that a user can be created via the ReqRes API using a POST request.
        /// Asserts that the returned response contains the name used in the payload.
        /// </summary>
        [Test]
        [Category("api")]
        [Category("api_create")]
        [Description("Verify user creation using ReqRes API")]
        public void CreateUserViaApiShouldReturnSuccess()
        {
            Console.WriteLine("Starting api create test");
            var apiClient = new ApiClient(apiKey: "reqres-free-v1", disableProxy: true);
            var rrs = new ReqResService(apiClient);
            string response = rrs.CreateUser("Morpheus", "Leader");
            Console.WriteLine("Here is the response: " + response);
            Assert.That(response, Does.Contain("Morpheus"), "Response did not contain expected name.");
        }

        /// <summary>
        /// Verifies that the user list fetched from page 2 contains a known user from the sample data.
        /// </summary>
        [Test]
        [Category("api")]
        [Description("Verify user list contains expected record from ReqRes API")]
        public async Task FetchUserListShouldReturnData()
        {
            var apiClient = new ApiClient(apiKey: "reqres-free-v1", disableProxy: true);
            var rrs = new ReqResService(apiClient);
            string response = await rrs.FetchUsersAsync(page: 2);
            Assert.That(response, Does.Contain("lindsay.ferguson@reqres.in"), "Known user not found in response.");
        }
    }
}
