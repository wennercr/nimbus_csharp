using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;     // NuGet: DotNetSeleniumExtras.WaitHelpers
using Nimbus.Framework.Utils;         // ConfigLoader (reads wait timeout)
using Nimbus.Framework.Utils.Logger;  // AllureHelper (logging to Allure / console)

namespace Nimbus.Framework.Core
{
    /// <summary>
    /// Base class for all Page Objects (POM) in Nimbus — implemented WITHOUT PageFactory.
    ///
    /// Key design choices:
    /// - Prefer <see cref="By"/>-based helpers (freshly locate elements at action time).
    /// - Provide legacy <see cref="IWebElement"/> overloads (marked [Obsolete])
    /// - Centralize explicit waits + logging so page classes stay thin and consistent.
    ///
    /// Why no PageFactory?
    /// - PageFactory caches IWebElement references, which go stale after DOM updates or navigation.
    /// - By-first promotes “find late, act late”, reducing StaleElementReference exceptions and flakiness.
    ///
    /// Usage pattern in pages:
    /// - Define private static readonly By locators (no IWebElement fields).
    /// - Call helpers like Visible(locator), Click(locator), SendKeys(locator, "text"), etc.
    /// - Keep assertions inside tests; keep actions/reads inside pages.
    /// </summary>
    public abstract class PageObjectBase
    {
        /// <summary>
        /// The WebDriver instance for this page object. Lifetime: owned by the test fixture.
        /// Thread-safety: do not share a single driver across parallel tests.
        /// </summary>
        protected readonly IWebDriver driver;

        /// <summary>
        /// Shared explicit wait for this page. Use for visibility/clickable checks.
        /// Keep timeouts reasonable to avoid masking real failures.
        /// </summary>
        protected readonly WebDriverWait wait;

        /// <summary>
        /// Construct with default timeout sourced from config (key: "wait.timeout.seconds", default 20s).
        /// Use this when most interactions on the page should share the same timeout.
        /// </summary>
        protected PageObjectBase(IWebDriver driver)
            : this(driver, GetDefaultTimeout())
        { }

        /// <summary>
        /// Construct with an explicit timeout (in seconds).
        /// Use this when a specific page is known to be slower/faster than the global default.
        /// </summary>
        protected PageObjectBase(IWebDriver driver, int timeoutInSeconds)
        {
            this.driver = driver ?? throw new ArgumentNullException(nameof(driver));
            // WebDriverWait uses polling; default polling is ~500ms in Selenium.Support.
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
        }

        /// <summary>
        /// Reads timeout from config. Falls back to 20s if missing or invalid.
        /// NOTE: In case of malformed value, we log and still use a sensible default.
        /// </summary>
        private static int GetDefaultTimeout()
        {
            var value = ConfigLoader.Get("wait.timeout.seconds");
            try
            {
                return !string.IsNullOrWhiteSpace(value) ? int.Parse(value) : 20;
            }
            catch (FormatException)
            {
                AllureHelper.LogStep("[WARN] Invalid wait.timeout.seconds in config. Using default: 20");
                return 20;
            }
        }

        // ======================================================================
        //                PREFERRED HELPERS (By-based; PageFactory-free)
        // ======================================================================

        /// <summary>
        /// Waits until an element located by <paramref name="locator"/> is visible (displayed & has size).
        /// Returns the (fresh) IWebElement for immediate action.
        /// Use this over caching IWebElement fields.
        /// </summary>
        protected IWebElement Visible(By locator)
        {
            try
            {
                var el = wait.Until(ExpectedConditions.ElementIsVisible(locator));
                return el;
            }
            catch (WebDriverTimeoutException)
            {
                AllureHelper.LogStep("[ERROR] Timeout waiting for: " + Describe(locator));
                throw;
            }
        }

        /// <summary>
        /// Waits until an element located by <paramref name="locator"/> is clickable (visible & enabled).
        /// Prefer this before clicking to avoid ElementNotInteractable errors.
        /// </summary>
        protected IWebElement Clickable(By locator)
        {
            try
            {
                var el = wait.Until(ExpectedConditions.ElementToBeClickable(locator));
                return el;
            }
            catch (WebDriverTimeoutException)
            {
                AllureHelper.LogStep("[ERROR] Timeout waiting clickable: " + Describe(locator));
                throw;
            }
        }

        /// <summary>
        /// Convenience wrapper if you just want to wait and not return the element.
        /// </summary>
        protected void WaitForVisibility(By locator) => _ = Visible(locator);

        /// <summary>
        /// Clears and types text into an input found by <paramref name="locator"/>.
        /// Re-finds element at action time to reduce staleness.
        /// </summary>
        protected void SendKeys(By locator, string text)
        {
            var el = Visible(locator); // ensure visible before typing
            el.Clear();
            el.SendKeys(text);
            AllureHelper.LogStep($"[ACTION] Sent the text: '{text}' to the elemenet:" + Describe(locator));

        }

        /// <summary>
        /// Clicks an element located by <paramref name="locator"/> after waiting for clickability.
        /// </summary>
        protected void Click(By locator)
        {
            Clickable(locator).Click();
            AllureHelper.LogStep("[ACTION] Clicked on the element: " + Describe(locator));
        }

        /// <summary>
        /// Performs a JavaScript click on an element located by <paramref name="locator"/>.
        /// Use sparingly (e.g., when regular click is blocked by overlays). Prefer normal Click first.
        /// </summary>
        protected void ClickUsingJS(By locator)
        {
            var el = driver.FindElement(locator); // quick find; JS click doesn’t need visibility check but keep it close
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el);
            AllureHelper.LogStep("[ACTION] JS Clicked on the element: " + Describe(locator));
        }

        /// <summary>
        /// Selects an option by its visible text in a &lt;select&gt; element located by <paramref name="locator"/>.
        /// </summary>
        protected void SelectDropdownByVisibleText(By locator, string visibleText)
        {
            new SelectElement(Visible(locator)).SelectByText(visibleText);
            AllureHelper.LogStep($"Selected the option by its visible text: '{visibleText}' on the following dropdown: " + Describe(locator));
        }


        /// <summary>
        /// Selects an option by its value attribute in a &lt;select&gt; element located by <paramref name="locator"/>.
        /// </summary>
        protected void SelectDropdownByValue(By locator, string value)
        {
            new SelectElement(Visible(locator)).SelectByValue(value);
            AllureHelper.LogStep($"Selected the option by its value: '{value}' on the following dropdown: " + Describe(locator));
        }

        /// <summary>
        /// Selects an option by its index in a &lt;select&gt; element located by <paramref name="locator"/>.
        /// </summary>
        protected void SelectDropdownByIndex(By locator, int index)
        {
            new SelectElement(Visible(locator)).SelectByIndex(index);
            AllureHelper.LogStep($"Selected the option by its index: '{index}' on the following dropdown: " + Describe(locator));
        }

        /// <summary>
        /// Returns the visible text of an element located by <paramref name="locator"/>.
        /// </summary>
        protected string GetText(By locator) => Visible(locator).Text;

        /// <summary>
        /// Returns true if at least one element exists in the DOM for <paramref name="locator"/>.
        /// Use for fast existence checks; this does NOT wait or require visibility.
        /// </summary>
        protected bool Exists(By locator) => driver.FindElements(locator).Count > 0;

        /// <summary>
        /// Current page title (handy for very quick assertions).
        /// </summary>
        public string GetPageTitle() => driver.Title;

        // ======================================================================
        //     LEGACY OVERLOADS (IWebElement-based) — to aid migration only
        // ======================================================================
        // These accept a previously-located IWebElement and try to use it.
        // They are marked [Obsolete] to nudge callers to switch to By-based helpers.
        // Keep them temporarily while converting pages/tests; remove later.

        /// <summary>
        /// Waits until the provided <see cref="IWebElement"/> reports Displayed=true.
        /// WARNING: If the reference is stale, this may loop until timeout. Prefer the By overload.
        /// </summary>
        [Obsolete("Prefer By-based overloads. This overload assumes element is fresh.")]
        protected void WaitForVisibility(IWebElement element)
        {
            try
            {
                AllureHelper.LogStep("[WAIT] Visibility(el): " + Describe(element));
                wait.Until(_ =>
                {
                    try { return element.Displayed ? element : null; }
                    catch (StaleElementReferenceException) { return null; }
                    catch (NoSuchElementException) { return null; }
                });
                AllureHelper.LogStep("[WAIT] Element visible.");
            }
            catch (WebDriverTimeoutException)
            {
                AllureHelper.LogStep("[ERROR] Timeout waiting for element visibility: " + Describe(element));
                throw;
            }
        }

        /// <summary>
        /// Clears and types into a provided <see cref="IWebElement"/>.
        /// WARNING: Prefer the By overload — this assumes the element isn’t stale.
        /// </summary>
        [Obsolete("Prefer By-based overloads.")]
        protected void SendKeys(IWebElement element, string text)
        {
            WaitForVisibility(element);
            AllureHelper.LogStep("[ACTION] Type(el): " + Describe(element) + $" | Text: {text}");
            element.Clear();
            element.SendKeys(text);
        }

        /// <summary>
        /// Clicks a provided <see cref="IWebElement"/> after waiting for visibility.
        /// WARNING: Prefer the By overload — this assumes the element isn’t stale.
        /// </summary>
        [Obsolete("Prefer By-based overloads.")]
        protected void Click(IWebElement element)
        {
            WaitForVisibility(element);
            AllureHelper.LogStep("[ACTION] Click(el): " + Describe(element));
            element.Click();
        }

        /// <summary>
        /// JavaScript click on a provided <see cref="IWebElement"/>.
        /// WARNING: Prefer the By overload to re-find fresh element when needed.
        /// </summary>
        [Obsolete("Prefer By-based overloads.")]
        protected void ClickUsingJS(IWebElement element)
        {
            AllureHelper.LogStep("[ACTION] JS Click(el): " + Describe(element));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
        }

        /// <summary>
        /// Gets text from a provided <see cref="IWebElement"/> after ensuring visibility.
        /// WARNING: Prefer the By overload — this assumes the element isn’t stale.
        /// </summary>
        [Obsolete("Prefer By-based overloads.")]
        protected string GetText(IWebElement element)
        {
            WaitForVisibility(element);
            return element.Text;
        }

        // ======================================================================
        //                         Describe / Debug Helpers
        // ======================================================================

        /// <summary>
        /// Human-friendly description of a By locator (used in logs).
        /// </summary>
        protected static string Describe(By locator)
            => locator?.ToString() ?? "[null locator]";

        /// <summary>
        /// Human-friendly description of an IWebElement (tag/name/id/text) for logs.
        /// Robust to stale/unknown elements.
        /// </summary>
        protected static string Describe(IWebElement element)
        {
            if (element == null) return "[null element]";
            try
            {
                var tag = element.TagName;
                var text = (element.Text ?? string.Empty).Trim();
                var id = element.GetAttribute("id");
                var name = element.GetAttribute("name");
                return $"<{tag} name='{name}' id='{id}' text='{text}'>";
            }
            catch (StaleElementReferenceException) { return "[STALE ELEMENT]"; }
            catch { return "[UNKNOWN ELEMENT]"; }
        }
    }
}
