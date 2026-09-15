using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace SeleniumTests;

public sealed class LowStockFlagTest
{
    [Fact]
    public void TC01_Sale_below_threshold_flags() => Run("US19-TC01", browser =>
        AssertFlagged(browser, Required("MEDZO_E2E_US19_TC01_MEDICINE")));

    [Fact]
    public void TC02_Purchase_return_below_threshold_flags() => Run("US19-TC02", browser =>
        AssertFlagged(browser, Required("MEDZO_E2E_US19_TC02_MEDICINE")));

    [Fact]
    public void TC03_Partial_failure_rolls_back_flag_too() => Run("US19-TC03", browser =>
    {
        var medicine = Required("MEDZO_E2E_US19_TC03_MEDICINE");
        AssertNotFlagged(browser, medicine);
        var state = InventoryState(browser, medicine);
        Assert.True(state.Quantity >= state.Threshold,
            $"Expected rollback to leave {medicine} healthy, but quantity {state.Quantity} is below threshold {state.Threshold}.");
    });

    [Fact]
    public void TC04_Duplicate_sale_idempotent() => Run("US19-TC04", browser =>
    {
        var medicine = Required("MEDZO_E2E_US19_TC04_MEDICINE");
        AssertFlagged(browser, medicine);

        browser.Navigate().GoToUrl($"{BaseUrl()}/inventory/sale-issues");
        var reference = Required("MEDZO_E2E_US19_TC04_SALE_REFERENCE");
        WaitUntil(browser, d => d.FindElement(By.XPath("//h1[normalize-space()='Sale stock updates']")));
        WaitUntil(browser, d => d.FindElements(By.CssSelector("main [role='status']")).Count == 0);
        var count = browser.FindElements(By.CssSelector("table tbody tr"))
            .Count(row => string.Equals(Cell(row, 1), reference, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, count);
    });

    [Fact]
    public void TC05_Stock_at_threshold_not_flagged() => Run("US19-TC05", browser =>
    {
        var medicine = Required("MEDZO_E2E_US19_TC05_MEDICINE");
        var before = InventoryState(browser, medicine);
        SetReorderThreshold(browser, medicine, before.Quantity);
        AssertNotFlagged(browser, medicine);
        var state = InventoryState(browser, medicine);
        Assert.Equal(state.Threshold, state.Quantity);
    });

    [Fact]
    public void TC06_Stock_one_below_threshold_flagged() => Run("US19-TC06", browser =>
    {
        var medicine = Required("MEDZO_E2E_US19_TC06_MEDICINE");
        var before = InventoryState(browser, medicine);
        SetReorderThreshold(browser, medicine, checked(before.Quantity + 1));
        var state = AssertFlagged(browser, medicine);
        Assert.Equal(state.Threshold - 1, state.Quantity);
    });

    [Fact]
    public void TC07_Replenishment_clears_flag() => Run("US19-TC07", browser =>
    {
        var medicine = Required("MEDZO_E2E_US19_TC07_MEDICINE");
        AssertNotFlagged(browser, medicine);
        var state = InventoryState(browser, medicine);
        Assert.True(state.Quantity >= state.Threshold,
            $"Expected replenishment to restore {medicine}, but quantity {state.Quantity} is below threshold {state.Threshold}.");
    });

    private static StockState AssertFlagged(IWebDriver browser, string medicine)
    {
        OpenLowStockSearch(browser, medicine);
        var row = WaitUntil(browser, d => d.FindElements(By.CssSelector("table tbody tr"))
            .FirstOrDefault(candidate => string.Equals(Cell(candidate, 0), medicine, StringComparison.OrdinalIgnoreCase)))!;
        var state = new StockState(Parse(Cell(row, 2)), Parse(Cell(row, 3)));
        Assert.True(state.Quantity < state.Threshold);
        Assert.Equal("Low stock", Cell(row, 6));
        return state;
    }

    private static void AssertNotFlagged(IWebDriver browser, string medicine)
    {
        browser.Navigate().GoToUrl($"{BaseUrl()}/inventory/low-stock");
        WaitUntil(browser, d => d.FindElement(By.Id("low-stock-search")));
        WaitUntil(browser, d =>
        {
            if (d.FindElements(By.CssSelector("main [role='status']")).Count > 0) return false;
            var mainText = d.FindElement(By.TagName("main")).Text;
            return d.FindElements(By.CssSelector("table tbody tr")).Count > 0
                || mainText.Contains("All medicines are at or above", StringComparison.OrdinalIgnoreCase);
        });

        var alert = browser.FindElements(By.CssSelector("main [role='alert']")).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(alert?.Text)) throw new InvalidOperationException(alert.Text);

        Assert.DoesNotContain(browser.FindElements(By.CssSelector("table tbody tr")),
            row => string.Equals(Cell(row, 0), medicine, StringComparison.OrdinalIgnoreCase));
    }

    private static StockState InventoryState(IWebDriver browser, string medicine)
    {
        browser.Navigate().GoToUrl($"{BaseUrl()}/inventory");
        var row = WaitUntil(browser, d => d.FindElements(By.CssSelector("table tbody tr"))
            .FirstOrDefault(candidate => string.Equals(Cell(candidate, 0), medicine, StringComparison.OrdinalIgnoreCase)))!;
        return new StockState(Parse(Cell(row, 2)), Parse(Cell(row, 3)));
    }

    private static void SetReorderThreshold(IWebDriver browser, string medicine, int threshold)
    {
        browser.Navigate().GoToUrl($"{BaseUrl()}/catalogue");
        WaitUntil(browser, d => d.FindElement(By.CssSelector("section[aria-busy='false']")));

        var row = WaitUntil(browser, d => d.FindElements(By.CssSelector("table tbody tr"))
            .FirstOrDefault(candidate => string.Equals(Cell(candidate, 0), medicine, StringComparison.OrdinalIgnoreCase)))!;
        row.FindElement(By.LinkText("Edit")).Click();

        var input = WaitUntil(browser, d => d.FindElement(By.CssSelector("input[name='reorderThreshold']")));
        input.Click();
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(Keys.Backspace);
        input.SendKeys(threshold.ToString());
        browser.FindElement(By.XPath("//button[normalize-space()='Save medicine']")).Click();

        Assert.Contains("Medicine updated successfully", WaitUntil(browser,
            d => d.FindElement(By.CssSelector("main [role='status']"))).Text);
        Assert.Equal(threshold.ToString(),
            browser.FindElement(By.CssSelector("input[name='reorderThreshold']")).GetAttribute("value"));
    }

    private static void OpenLowStockSearch(IWebDriver browser, string medicine)
    {
        browser.Navigate().GoToUrl($"{BaseUrl()}/inventory/low-stock");
        var search = WaitUntil(browser, d => d.FindElement(By.Id("low-stock-search")));
        search.Click();
        search.SendKeys(Keys.Control + "a");
        search.SendKeys(Keys.Backspace);
        search.SendKeys(medicine);
        browser.FindElement(By.XPath("//form[@role='search']//button[@type='submit']")).Click();
        WaitUntil(browser, d =>
        {
            if (d.FindElements(By.CssSelector("main [role='status']")).Count > 0) return false;
            var hasMatchingRow = d.FindElements(By.CssSelector("table tbody tr"))
                .Any(row => string.Equals(Cell(row, 0), medicine, StringComparison.OrdinalIgnoreCase));
            var hasNoMatchMessage = d.FindElement(By.TagName("main")).Text
                .Contains("No low-stock medicines match", StringComparison.OrdinalIgnoreCase);
            return hasMatchingRow || hasNoMatchMessage;
        });
        var alert = browser.FindElements(By.CssSelector("main [role='alert']")).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(alert?.Text)) throw new InvalidOperationException(alert.Text);
    }

    private static int Parse(string value) =>
        int.TryParse(value, out var parsed) ? parsed : throw new InvalidOperationException($"Expected integer stock value, but found '{value}'.");

    private static string Cell(IWebElement row, int index) =>
        row.FindElements(By.CssSelector("td"))[index].Text.Trim();

    private static void Run(string testCase, Action<ChromeDriver> test)
    {
        using var browser = CreateBrowser();
        try
        {
            Login(browser);
            test(browser);
        }
        finally { SaveScreenshot(browser, testCase); }
    }

    private static ChromeDriver CreateBrowser()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1440,1000");
        if (Environment.GetEnvironmentVariable("MEDZO_E2E_HEADLESS") == "true") options.AddArgument("--headless=new");
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
            if (!string.IsNullOrWhiteSpace(alert?.Text)) throw new InvalidOperationException($"Login was rejected: {alert.Text}");
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

            if (DateTime.UtcNow >= deadline) throw new WebDriverTimeoutException("Timed out waiting for the low-stock page state.");
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

    private sealed record StockState(int Quantity, int Threshold);
}
