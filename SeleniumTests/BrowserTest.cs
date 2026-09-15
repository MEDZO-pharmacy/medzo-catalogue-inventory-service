using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace SeleniumTests;

public sealed class BrowserTest
{
    [Fact] public void TC01_Record_valid_batch_success() => Run("TC01", browser =>
    {
        var batch = UniqueBatch("TC01");
        var message = Record(browser, batch);
        Assert.Contains(batch, message);
        Assert.Contains("stock is now", message.ToLowerInvariant());
    });

    [Fact] public void TC02_Duplicate_batch_number_error() => Run("TC02", browser =>
    {
        var batch = UniqueBatch("TC02");
        Record(browser, batch);
        FillForm(browser, batch, FutureExpiry());
        Submit(browser);
        Assert.Contains("already exists", PageAlert(browser).ToLowerInvariant());
    });

    [Fact] public void TC03_Empty_batch_number_error() => Run("TC03", browser =>
    {
        FillForm(browser, string.Empty, FutureExpiry());
        Submit(browser);
        Assert.Equal("Batch number is required.", FieldError(browser, "batchNumber"));
    });

    [Fact] public void TC04_Empty_expiry_date_error() => Run("TC04", browser =>
    {
        FillForm(browser, UniqueBatch("TC04"), null);
        Submit(browser);
        Assert.Equal("Expiry date must be in the future.", FieldError(browser, "expiryDate"));
    });

    [Fact] public void TC05_Past_expiry_date_handling() => Run("TC05", browser =>
    {
        FillForm(browser, UniqueBatch("TC05"), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        Submit(browser);
        Assert.Equal("Expiry date must be in the future.", FieldError(browser, "expiryDate"));
    });

    [Fact] public void TC06_Prevent_rerecording_same_batch() => Run("TC06", browser =>
    {
        var batch = UniqueBatch("TC06");
        Record(browser, batch);
        browser.Navigate().Refresh();
        WaitUntil(browser, MedicinesLoaded);
        FillForm(browser, batch, FutureExpiry());
        Submit(browser);
        Assert.Contains("stock was not changed", PageAlert(browser).ToLowerInvariant());
    });

    private static void Run(string testCase, Action<ChromeDriver> test)
    {
        using var browser = CreateBrowser();
        try
        {
            Login(browser);
            browser.Navigate().GoToUrl($"{BaseUrl()}/inventory/batches/new");
            WaitUntil(browser, MedicinesLoaded);
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
        WaitUntil(browser, d => d.FindElement(By.Id("login-identifier"))).SendKeys(Required("MEDZO_E2E_STAFF", "MEDZO_E2E_STAFF_A"));
        browser.FindElement(By.Id("login-password")).SendKeys(Required("MEDZO_E2E_PASSWORD", "MEDZO_E2E_PASSWORD_A"));
        browser.FindElement(By.CssSelector("button[type='submit']")).Click();
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (!browser.Url.EndsWith("/login", StringComparison.OrdinalIgnoreCase)) return;
            var alert = browser.FindElements(By.CssSelector("[role='alert']")).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(alert?.Text)) throw new InvalidOperationException($"Login was rejected: {alert.Text}");
            Thread.Sleep(200);
        }
        throw new WebDriverTimeoutException("Login did not complete. Check Auth API and credentials.");
    }

    private static void FillForm(IWebDriver browser, string batchNumber, DateOnly? expiry)
    {
        var select = browser.FindElement(By.Name("medicineId"));
        var medicineId = Required("MEDZO_E2E_MEDICINE_ID");
        var option = select.FindElements(By.TagName("option")).SingleOrDefault(x => x.GetAttribute("value") == medicineId)
            ?? throw new InvalidOperationException("MEDZO_E2E_MEDICINE_ID is not an active medicine.");
        option.Click();
        var batch = browser.FindElement(By.Name("batchNumber"));
        batch.Clear();
        if (batchNumber.Length > 0) batch.SendKeys(batchNumber);
        browser.FindElement(By.Name("description")).SendKeys("Selenium .NET test");
        if (expiry is not null) SetDate(browser, browser.FindElement(By.Name("expiryDate")), expiry.Value);
        var quantity = browser.FindElement(By.Name("quantity"));
        quantity.Clear(); quantity.SendKeys("5");
        browser.FindElement(By.Name("sourceReference")).SendKeys($"SEL-{Guid.NewGuid():N}"[..12]);
    }

    private static void SetDate(IWebDriver browser, IWebElement input, DateOnly value) =>
        ((IJavaScriptExecutor)browser).ExecuteScript("""
            const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
            setter.call(arguments[0], arguments[1]);
            arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
            arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
            """, input, value.ToString("yyyy-MM-dd"));

    private static string Record(IWebDriver browser, string batchNumber)
    {
        FillForm(browser, batchNumber, FutureExpiry());
        Submit(browser);
        return WaitUntil(browser, d => d.FindElement(By.CssSelector("p[role='status'][aria-live='polite']"))).Text;
    }

    private static void Submit(IWebDriver browser) => browser.FindElement(By.XPath("//button[normalize-space()='Record stock batch']")).Click();
    private static string PageAlert(IWebDriver browser) => WaitUntil(browser, d => d.FindElement(By.CssSelector("main > div > p[role='alert']"))).Text;
    private static string FieldError(IWebDriver browser, string field) => WaitUntil(browser, d => d.FindElement(By.XPath($"//*[@name='{field}']/following-sibling::*[@role='alert']"))).Text;
    private static DateOnly FutureExpiry() => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(180));
    private static string UniqueBatch(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..15].ToUpperInvariant();
    private static string BaseUrl() => Required("MEDZO_E2E_FRONTEND_URL").TrimEnd('/');
    private static bool MedicinesLoaded(IWebDriver browser) => browser.FindElement(By.Name("medicineId")).FindElements(By.TagName("option")).Count > 1;

    private static T WaitUntil<T>(IWebDriver browser, Func<IWebDriver, T> action)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (true)
        {
            try { var result = action(browser); if (result is not bool ready || ready) return result; }
            catch (NoSuchElementException) when (DateTime.UtcNow < deadline) { }
            if (DateTime.UtcNow >= deadline) throw new WebDriverTimeoutException("Timed out waiting for the page state.");
            Thread.Sleep(200);
        }
    }

    private static void SaveScreenshot(IWebDriver browser, string testCase)
    {
        try
        {
            var directory = Environment.GetEnvironmentVariable("MEDZO_EVIDENCE_DIR") ?? Path.Combine(AppContext.BaseDirectory, "evidence", "selenium");
            Directory.CreateDirectory(directory);
            ((ITakesScreenshot)browser).GetScreenshot().SaveAsFile(Path.Combine(directory, $"{testCase}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png"));
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
