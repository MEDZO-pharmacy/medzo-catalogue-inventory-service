using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace SeleniumTests;

public sealed class SaleStockUpdateTest
{
    [Fact]
    public void TC01_Record_sale_decreases_stock() => Run("US05-TC01", browser =>
    {
        var row = FindSale(browser, Required("MEDZO_E2E_SALE_REFERENCE"));
        Assert.Equal(Required("MEDZO_E2E_SALE_QUANTITY"), Cell(row, 5).TrimStart('\u2212', '-'));
        Assert.Equal(Required("MEDZO_E2E_SALE_NEW_BALANCE"), Cell(row, 6));
    });

    [Fact]
    public void TC02_Partial_failure_rolls_back() => Run("US05-TC02", browser =>
    {
        var failedReference = Required("MEDZO_E2E_FAILED_SALE_REFERENCE");
        Assert.DoesNotContain(SaleRows(browser), row =>
            string.Equals(Cell(row, 1), failedReference, StringComparison.OrdinalIgnoreCase));
        AssertInventoryBalance(browser, Required("MEDZO_E2E_SALE_MEDICINE"),
            Required("MEDZO_E2E_ROLLBACK_BALANCE"));
    });

    [Fact]
    public void TC03_Retry_does_not_double_decrement() => Run("US05-TC03", browser =>
    {
        var reference = Required("MEDZO_E2E_RETRY_SALE_REFERENCE");
        var matchingRows = SaleRows(browser).Where(row =>
            string.Equals(Cell(row, 1), reference, StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Single(matchingRows);
        Assert.Equal(Required("MEDZO_E2E_RETRY_NEW_BALANCE"), Cell(matchingRows[0], 6));
    });

    [Fact]
    public void TC04_Insufficient_stock_blocks_sale() => Run("US05-TC04", browser =>
    {
        var rejectedReference = Required("MEDZO_E2E_INSUFFICIENT_SALE_REFERENCE");
        Assert.DoesNotContain(SaleRows(browser), row =>
            string.Equals(Cell(row, 1), rejectedReference, StringComparison.OrdinalIgnoreCase));
        AssertInventoryBalance(browser, Required("MEDZO_E2E_SALE_MEDICINE"),
            Required("MEDZO_E2E_INSUFFICIENT_BALANCE"));
    });

    private static void Run(string testCase, Action<ChromeDriver> assertion)
    {
        using var browser = CreateBrowser();
        try
        {
            Login(browser);
            OpenSaleUpdates(browser);
            assertion(browser);
        }
        finally
        {
            SaveScreenshot(browser, testCase);
        }
    }

    private static void OpenSaleUpdates(IWebDriver browser)
    {
        browser.Navigate().GoToUrl($"{BaseUrl()}/inventory/sale-issues");
        WaitUntil(browser, d => d.FindElements(By.CssSelector("main [role='alert']")).Count > 0
            || d.FindElements(By.CssSelector("table tbody tr")).Count > 0
            || d.PageSource.Contains("No sale stock updates have been processed yet.", StringComparison.Ordinal));

        var alert = browser.FindElements(By.CssSelector("main [role='alert']")).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(alert?.Text))
            throw new InvalidOperationException($"Sale updates could not be loaded: {alert.Text}");
    }

    private static IWebElement FindSale(IWebDriver browser, string reference) =>
        WaitUntil(browser, d => SaleRows(d).FirstOrDefault(row =>
            string.Equals(Cell(row, 1), reference, StringComparison.OrdinalIgnoreCase)))!;

    private static IReadOnlyList<IWebElement> SaleRows(IWebDriver browser) =>
        browser.FindElements(By.CssSelector("table tbody tr"));

    private static string Cell(IWebElement row, int index) =>
        row.FindElements(By.CssSelector("td"))[index].Text.Trim();

    private static void AssertInventoryBalance(IWebDriver browser, string medicine, string expected)
    {
        browser.Navigate().GoToUrl($"{BaseUrl()}/inventory");
        var row = WaitUntil(browser, d => d.FindElements(By.CssSelector("table tbody tr"))
            .FirstOrDefault(item => string.Equals(Cell(item, 0), medicine, StringComparison.OrdinalIgnoreCase)))!;
        Assert.Equal(expected, Cell(row, 2));
    }

    private static ChromeDriver CreateBrowser()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1440,1000");
        if (Environment.GetEnvironmentVariable("MEDZO_E2E_HEADLESS") == "true")
            options.AddArgument("--headless=new");
        return new ChromeDriver(options);
    }

    private static void Login(IWebDriver browser)
    {
        browser.Navigate().GoToUrl($"{BaseUrl()}/login");
        WaitUntil(browser, d => d.FindElement(By.Id("login-identifier")))
            .SendKeys(Required("MEDZO_E2E_STAFF", "MEDZO_E2E_STAFF_A"));
        browser.FindElement(By.Id("login-password"))
            .SendKeys(Required("MEDZO_E2E_PASSWORD", "MEDZO_E2E_PASSWORD_A"));
        browser.FindElement(By.CssSelector("button[type='submit']")).Click();

        WaitUntil(browser, d =>
        {
            var alert = d.FindElements(By.CssSelector("[role='alert']")).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(alert?.Text))
                throw new InvalidOperationException($"Login was rejected: {alert.Text}");
            return !d.Url.EndsWith("/login", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string BaseUrl() => Required("MEDZO_E2E_FRONTEND_URL").TrimEnd('/');

    private static T WaitUntil<T>(IWebDriver browser, Func<IWebDriver, T> action)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (true)
        {
            try
            {
                var result = action(browser);
                if (result is not null && (result is not bool ready || ready)) return result;
            }
            catch (NoSuchElementException) when (DateTime.UtcNow < deadline) { }
            catch (StaleElementReferenceException) when (DateTime.UtcNow < deadline) { }

            if (DateTime.UtcNow >= deadline)
                throw new WebDriverTimeoutException("Timed out waiting for the sale/inventory page state.");
            Thread.Sleep(200);
        }
    }

    private static void SaveScreenshot(IWebDriver browser, string testCase)
    {
        try
        {
            var directory = Environment.GetEnvironmentVariable("MEDZO_EVIDENCE_DIR")
                ?? Path.Combine(AppContext.BaseDirectory, "evidence", "selenium");
            Directory.CreateDirectory(directory);
            ((ITakesScreenshot)browser).GetScreenshot().SaveAsFile(
                Path.Combine(directory, $"{testCase}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png"));
        }
        catch (WebDriverException) { }
    }

    private static string Required(params string[] names)
    {
        foreach (var name in names)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        throw new InvalidOperationException($"Set one of these E2E environment variables: {string.Join(", ", names)}");
    }
}
