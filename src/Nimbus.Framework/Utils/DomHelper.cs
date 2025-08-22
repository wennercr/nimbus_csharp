
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

/// <summary>
/// Utility class for common DOM-related operations in the Nimbus framework.
/// </summary>
namespace Nimbus.Framework.Utils
{
    public static class DomHelper
    {
        /// <summary>
        /// Returns the row at the specified index (0-based).
        ///
        /// <param name="table">WebElement representing the &lt;table&gt;</param>
        /// <param name="index">Index of the row (starting at 0)</param>
        /// <returns>WebElement of the row, or null if out of bounds</returns>
        /// </summary>
        public static IWebElement? GetRowByIndex(IWebElement table, int index)
        {
            var rows = table.FindElements(By.TagName("tr"));
            return (index >= 0 && index < rows.Count) ? rows[index] : null;
        }

        /// <summary>
        /// Returns the first row that contains the given text.
        ///
        /// <param name="table">WebElement representing the &lt;table&gt;</param>
        /// <param name="text">Substring to search for in any row's text</param>
        /// <returns>WebElement of the row, or null if not found</returns>
        /// </summary>
        public static IWebElement? GetRowContainingText(IWebElement table, string text)
        {
            var rows = table.FindElements(By.TagName("tr"));
            foreach (var row in rows)
            {
                if (row.Text.ToLower().Contains(text.ToLower()))
                {
                    return row;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the cell at the given column index within the first row that contains the given row text.
        ///
        /// <param name="table">WebElement representing the &lt;table&gt;</param>
        /// <param name="rowText">Substring to match in the row</param>
        /// <param name="cellIndex">Index of the cell (0-based)</param>
        /// <returns>WebElement of the cell, or null if not found</returns>
        /// </summary>
        public static IWebElement? GetCell(IWebElement table, string rowText, int cellIndex)
        {
            var row = GetRowContainingText(table, rowText);
            if (row == null) return null;

            var cells = row.FindElements(By.TagName("td"));
            return (cellIndex >= 0 && cellIndex < cells.Count) ? cells[cellIndex] : null;
        }

        /// <summary>
        /// Verifies that the table's header row matches the given list of expected headers.
        ///
        /// <param name="table">WebElement representing the &lt;table&gt;</param>
        /// <param name="expectedHeaders">List of expected header titles in order</param>
        /// <returns>true if all headers match in order, false otherwise</returns>
        /// </summary>
        public static bool VerifyTableHeaders(IWebElement table, List<string> expectedHeaders)
        {
            var headers = table.FindElements(By.CssSelector("thead tr th"));
            if (headers.Count != expectedHeaders.Count) return false;

            for (int i = 0; i < headers.Count; i++)
            {
                string actual = headers[i].Text.Trim();
                string expected = expectedHeaders[i].Trim();
                if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the full text of a row by index.
        ///
        /// <param name="table">WebElement representing the &lt;table&gt;</param>
        /// <param name="index">Index of the row (0-based)</param>
        /// <returns>Full text of the row, or null if not found</returns>
        /// </summary>
        public static string? GetRowTextByIndex(IWebElement table, int index)
        {
            var row = GetRowByIndex(table, index);
            return row != null ? row.Text.Trim() : null;
        }

        /// <summary>
        /// Gets the full text of the first row that contains a specific keyword.
        ///
        /// <param name="table">WebElement representing the &lt;table&gt;</param>
        /// <param name="keyword">Substring to search for</param>
        /// <returns>Full text of the row, or null if not found</returns>
        /// </summary>
        public static string? GetRowTextByKeyword(IWebElement table, string keyword)
        {
            var row = GetRowContainingText(table, keyword);
            return row != null ? row.Text.Trim() : null;
        }

        /// <summary>
        /// Gets the text content of each cell within the given row.
        ///
        /// <param name="row">WebElement representing a &lt;tr&gt;</param>
        /// <returns>List of trimmed cell values</returns>
        /// </summary>
        public static List<string> GetCellTextsFromRow(IWebElement row)
        {
            return row.FindElements(By.TagName("td"))
                      .Select(cell => cell.Text.Trim())
                      .ToList();
        }

        /// <summary>
        /// Waits until a specific number of elements are visible in the DOM.
        ///
        /// <param name="driver">WebDriver instance</param>
        /// <param name="by">Locator to match</param>
        /// <param name="expectedCount">Number of elements to wait for</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <returns>List of WebElements once count is matched</returns>
        /// </summary>
        public static IReadOnlyCollection<IWebElement> WaitForElementsCount(
            IWebDriver driver, By by, int expectedCount, int timeout)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
            return wait.Until(driver =>
                {
                    var elements = driver.FindElements(by);
                    return elements.Count == expectedCount ? elements : null;
                });
        }
    }
}
