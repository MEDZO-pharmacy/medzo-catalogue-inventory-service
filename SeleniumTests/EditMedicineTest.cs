using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace SeleniumTests;

public sealed class EditMedicineTest
{
    [Fact]
    public void TC01_Edit_medicine_success()
    {
        using var browser = SignedInBrowser("MEDZO_E2E_STAFF_A", "MEDZO_E2E_PASSWORD_A");
        try
        {
            OpenTargetForEdit(browser);
            var genericName = $"Selenium Edit {DateTime.UtcNow:yyyyMMddHHmmss}";
            Set(Input(browser, "genericName"), genericName);
            Submit(browser);

            Assert.Contains("Medicine updated successfully", WaitUntil(browser,
                d => d.FindElement(By.CssSelector("main [role='status']"))).Text);
            Assert.Equal(genericName, Input(browser, "genericName").GetAttribute("value"));
        }
        finally { SaveScreenshot(browser, "US07-TC01"); }
    }

    [Fact]
    public void TC02_Empty_required_field_blocks_save()
    {
        using var browser = SignedInBrowser("MEDZO_E2E_STAFF_A", "MEDZO_E2E_PASSWORD_A");
        try
        {
            OpenTargetForEdit(browser);
            Set(Input(browser, "manufacturer"), string.Empty);
            Submit(browser);

            Assert.Equal("Manufacturer is required.", WaitUntil(browser,
                d => d.FindElement(By.Id("manufacturer-error"))).Text);
            Assert.Equal("true", Input(browser, "manufacturer").GetAttribute("aria-invalid"));
            Assert.Empty(browser.FindElements(By.CssSelector("main [role='status']")));
        }
        finally { SaveScreenshot(browser, "US07-TC02"); }
    }

    [Fact]
    public void TC03_Concurrent_edits_conflict_handling()
    {
        using var first = SignedInBrowser("MEDZO_E2E_STAFF_A", "MEDZO_E2E_PASSWORD_A");
        using var second = SignedInBrowser("MEDZO_E2E_STAFF_B", "MEDZO_E2E_PASSWORD_B");
        try
        {
            OpenTargetForEdit(first);
            OpenTargetForEdit(second);

            var firstThreshold = Required("MEDZO_E2E_FIRST_THRESHOLD");
            var secondThreshold = Required("MEDZO_E2E_SECOND_THRESHOLD");
            Assert.NotEqual(firstThreshold, secondThreshold);

            Set(Input(first, "reorderThreshold"), firstThreshold);
            Set(Input(second, "reorderThreshold"), secondThreshold);

            Submit(first);
            Assert.Contains("Medicine updated successfully", WaitUntil(first,
                d => d.FindElement(By.CssSelector("main [role='status']"))).Text);

            Submit(second);
            Assert.Equal("This medicine was changed by another user", WaitUntil(second,
                d => d.FindElement(By.XPath("//div[@role='alert']//h2"))).Text);

            second.FindElement(By.XPath("//button[normalize-space()='Reload latest version']")).Click();
            WaitUntil(second, d => Input(d, "reorderThreshold").GetAttribute("value") == firstThreshold);
            Assert.Equal(firstThreshold, Input(second, "reorderThreshold").GetAttribute("value"));
        }
        finally
        {
            SaveScreenshot(first, "US07-TC03-session-A");
            SaveScreenshot(second, "US07-TC03-session-B");
        }
    }

    [Fact]
    public void TC04_Cancel_discards_changes()
    {
        using var browser = SignedInBrowser("MEDZO_E2E_STAFF_A", "MEDZO_E2E_PASSWORD_A");
        try
        {
            OpenTargetForEdit(browser);
            var originalManufacturer = Input(browser, "manufacturer").GetAttribute("value");
            Set(Input(browser, "manufacturer"), "UNSAVED SELENIUM CHANGE");

            browser.FindElement(By.LinkText("Back to catalogue")).Click();
            WaitUntil(browser, d => d.Url.EndsWith("/catalogue", StringComparison.OrdinalIgnoreCase));
            OpenTargetForEdit(browser);

            Assert.Equal(originalManufacturer, Input(browser, "manufacturer").GetAttribute("value"));
        }
        finally { SaveScreenshot(browser, "US07-TC04"); }
    }

    [Fact(Skip = "The current medicine API and Edit Medicine UI expose no updated timestamp or medicine audit log.")]
    public void TC05_Edit_updates_timestamp_and_audit_log() { }

    private static ChromeDriver SignedInBrowser(string userVariable, string passwordVariable)
    {
        var browser = CreateBrowser();
        try
        {
            Login(browser, Required(userVariable, userVariable == "MEDZO_E2E_STAFF_A" ? "MEDZO_E2E_STAFF" : userVariable),
                Required(passwordVariable, passwordVariable == "MEDZO_E2E_PASSWORD_A" ? "MEDZO_E2E_PASSWORD" : passwordVariable));
            return browser;
        }
        catch
        {
            browser.Dispose();
            throw;
        }
    }

    private static void OpenTargetForEdit(IWebDriver browser)
    {
        var medicine = Required("MEDZO_E2E_EDIT_MEDICINE_NAME");
        browser.Navigate().GoToUrl($"{BaseUrl()}/catalogue");
        WaitUntil(browser, d => d.FindElement(By.CssSelector("section[aria-busy='false']")));

        var search = browser.FindElement(By.CssSelector("form[role='search'] input[type='search']"));
        Set(search, medicine);
        browser.FindElement(By.XPath("//form[@role='search']//button[@type='submit']")).Click();

        var row = WaitUntil(browser, d => d.FindElements(By.CssSelector("table tbody tr"))
            .FirstOrDefault(candidate => string.Equals(Cell(candidate, 0), medicine, StringComparison.OrdinalIgnoreCase)))!;
        row.FindElement(By.LinkText("Edit")).Click();
        WaitUntil(browser, d => d.FindElement(By.XPath("//h1[normalize-space()='Edit medicine']")));
        WaitUntil(browser, d => !string.IsNullOrWhiteSpace(Input(d, "name").GetAttribute("value")));
    }

    private static IWebElement Input(IWebDriver browser, string name) =>
        browser.FindElement(By.CssSelector($"input[name='{name}']"));

    private static string Cell(IWebElement row, int index) =>
        row.FindElements(By.CssSelector("td"))[index].Text.Trim();

    private static void Set(IWebElement element, string value)
    {
        element.Click();
        element.SendKeys(Keys.Control + "a");
        element.SendKeys(Keys.Backspace);
        if (value.Length > 0) element.SendKeys(value);
    }

    private static void Submit(IWebDriver browser) =>
        browser.FindElement(By.XPath("//button[normalize-space()='Save medicine']")).Click();

    private static ChromeDriver CreateBrowser()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1440,1000");
        if (Environment.GetEnvironmentVariable("MEDZO_E2E_HEADLESS") == "true") options.AddArgument("--headless=new");
        return new ChromeDriver(options);
    }

    private static void Login(IWebDriver browser, string identifier, string password)
    {
        browser.Navigate().GoToUrl($"{BaseUrl()}/login");
        WaitUntil(browser, d => d.FindElement(By.Id("login-identifier"))).SendKeys(identifier);
        browser.FindElement(By.Id("login-password")).SendKeys(password);
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

            if (DateTime.UtcNow >= deadline) throw new WebDriverTimeoutException("Timed out waiting for the Edit Medicine page state.");
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
