using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace SeleniumTests;

public sealed class CatalogueSearchTest
{
    [Fact]
    public void TC01_Partial_name_search() => Run("US03-TC01", browser =>
    {
        var fullName = Required("MEDZO_E2E_SEARCH_MEDICINE_NAME");
        var partialName = fullName[..Math.Max(1, fullName.Length / 2)];

        Search(browser, partialName);

        Assert.Contains(ResultNames(browser), name =>
            name.Contains(partialName, StringComparison.OrdinalIgnoreCase));
    });

    [Fact]
    public void TC02_Full_name_search() => Run("US03-TC02", browser =>
    {
        var fullName = Required("MEDZO_E2E_SEARCH_MEDICINE_NAME");

        Search(browser, fullName);

        Assert.Contains(ResultNames(browser), name =>
            string.Equals(name, fullName, StringComparison.OrdinalIgnoreCase));
    });

    [Fact]
    public void TC03_Case_insensitive_search() => Run("US03-TC03", browser =>
    {
        var fullName = Required("MEDZO_E2E_SEARCH_MEDICINE_NAME");

        Search(browser, fullName.ToUpperInvariant());

        Assert.Contains(ResultNames(browser), name =>
            string.Equals(name, fullName, StringComparison.OrdinalIgnoreCase));
    });

    [Fact]
    public void TC04_No_results_found() => Run("US03-TC04", browser =>
    {
        Search(browser, $"NO-MEDICINE-{Guid.NewGuid():N}");

        Assert.Equal("No medicines found", WaitUntil(browser,
            d => d.FindElement(By.XPath("//section//h2[normalize-space()='No medicines found']"))).Text);
        Assert.Empty(browser.FindElements(By.CssSelector("table tbody tr")));
    });

    [Fact]
    public void TC05_Clear_search_restores_full_list() => Run("US03-TC05", browser =>
    {
        var initialNames = ResultNames(browser);
        Assert.NotEmpty(initialNames);
        Search(browser, Required("MEDZO_E2E_SEARCH_MEDICINE_NAME"));

        browser.FindElement(By.XPath("//form[@role='search']//button[@type='button'][contains(., 'Clear')]")).Click();
        WaitUntil(browser, d =>
            string.IsNullOrEmpty(d.FindElement(By.CssSelector("form[role='search'] input[type='search']")).GetAttribute("value"))
            && ResultNames(d).Count == initialNames.Count);

        Assert.Equal(initialNames, ResultNames(browser));
    });

    [Fact]
    public void TC06_Special_characters_in_search() => Run("US03-TC06", browser =>
    {
        const string specialCharacters = "!@#$%^&*()";

        Search(browser, specialCharacters);

        Assert.Equal("No medicines found", WaitUntil(browser,
            d => d.FindElement(By.XPath("//section//h2[normalize-space()='No medicines found']"))).Text);
        Assert.Empty(browser.FindElements(By.CssSelector("table tbody tr")));
    });

    private static void Run(string testCase, Action<ChromeDriver> test)
    {
        using var browser = CreateBrowser();
        try
        {
            Login(browser);
            browser.Navigate().GoToUrl($"{BaseUrl()}/catalogue");
            WaitUntil(browser, d => d.FindElement(By.CssSelector("section[aria-live='polite'][aria-busy='false']")));
            WaitUntil(browser, d => ResultNames(d).Count > 0);
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

    private static void Search(IWebDriver browser, string query)
    {
        var input = browser.FindElement(By.CssSelector("form[role='search'] input[type='search']"));
        input.Clear();
        input.SendKeys(query);
        browser.FindElement(By.XPath("//form[@role='search']//button[@type='submit']")).Click();

        WaitUntil(browser, d =>
        {
            var section = d.FindElement(By.CssSelector("section[aria-live='polite']"));
            return section.GetAttribute("aria-busy") == "false"
                && section.Text.Contains(query, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static IReadOnlyList<string> ResultNames(IWebDriver browser) =>
        browser.FindElements(By.CssSelector("table tbody tr td:first-child"))
            .Select(cell => cell.Text.Trim()).ToList();

    private static string BaseUrl() => Required("MEDZO_E2E_FRONTEND_URL").TrimEnd('/');

    private static T WaitUntil<T>(IWebDriver browser, Func<IWebDriver, T> action)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (true)
        {
            try
            {
                var result = action(browser);
                if (result is not bool ready || ready) return result;
            }
            catch (NoSuchElementException) when (DateTime.UtcNow < deadline) { }

            if (DateTime.UtcNow >= deadline) throw new WebDriverTimeoutException("Timed out waiting for the catalogue page state.");
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
