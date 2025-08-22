using NUnit.Framework;
using Nimbus.Framework.Utils.Logger;

[SetUpFixture]
public sealed class AllureBootstrap
{
    [OneTimeSetUp]
    public void EnsureAllureResultsFolder()
    {
        TestContext.Progress.WriteLine($"I AM IN MY BEFORE ALL BOOTSTRAP");

        // This matches allureConfig.json: "target/allure-results"
        var path = Path.Combine(AppContext.BaseDirectory, "target", "allure-results");
        Directory.CreateDirectory(path);
        TestContext.Progress.WriteLine($"[Nimbus] Allure results directory: {path}");
    }

    [OneTimeTearDown]
    public void BuildAllureReport()
    {
        TestContext.Progress.WriteLine("I AM IN MY ONE TIME TEARDOWN BOOTSTRAP");

        ReportManager.GenerateHtmlReport();
    }
}
