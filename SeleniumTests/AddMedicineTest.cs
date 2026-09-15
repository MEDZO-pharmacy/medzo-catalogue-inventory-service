using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace SeleniumTests;

public sealed class AddMedicineTest
{
    [Fact]
    public void TC01_Add_medicine_success() => RunAuthorized("US06-TC01", browser =>
    {
        var name = $"Selenium Medicine {Guid.NewGuid():N}";
        FillValidForm(browser, name, $"Generic {Guid.NewGuid():N}", "Selenium Manufacturer");
        Submit(browser);

        Assert.Equal("Medicine added successfully", WaitUntil(browser,
            d => d.FindElement(By.XPath("//h1[normalize-space()='Medicine added successfully']"))).Text);
        Assert.Contains(name, browser.FindElement(By.CssSelector("main [role='status']")).Text);
    });

    [Fact]
    public void TC02_Missing_name_rejected() => RunAuthorized("US06-TC02", browser =>
    {
        FillValidForm(browser, string.Empty, "Missing Name Generic", "Selenium Manufacturer");
        Submit(browser);

        Assert.Equal("Medicine name is required.", WaitUntil(browser,
            d => d.FindElement(By.Id("name-error"))).Text);
        Assert.Equal("true", Input(browser, "name").GetAttribute("aria-invalid"));
        Assert.Equal("Add medicine", browser.FindElement(By.TagName("h1")).Text);
    });

    [Fact]
    public void TC03_Negative_unit_price_rejected() => RunAuthorized("US06-TC03", browser =>
    {
        FillValidForm(browser, $"Negative Price {Guid.NewGuid():N}", "Negative Price Generic", "Selenium Manufacturer", "-0.01");
        Submit(browser);

        Assert.Equal("Unit price must be zero or greater.", WaitUntil(browser,
            d => d.FindElement(By.Id("unitPrice-error"))).Text);
        Assert.Equal("true", Input(browser, "unitPrice").GetAttribute("aria-invalid"));
    });

    [Fact]
    public void TC04_Duplicate_warning_cancel_and_confirm() => RunAuthorized("US06-TC04", browser =>
    {
        var name = Required("MEDZO_E2E_DUPLICATE_MEDICINE_NAME");
        var manufacturer = Required("MEDZO_E2E_DUPLICATE_MANUFACTURER");
        FillValidForm(browser, name, "Duplicate Selenium Generic", manufacturer);
        Submit(browser);

        Assert.Equal("Potential duplicate medicine", WaitUntil(browser,
            d => d.FindElement(By.XPath("//h2[normalize-space()='Potential duplicate medicine']"))).Text);

        browser.FindElement(By.XPath("//button[normalize-space()='Review form']")).Click();
        WaitUntil(browser, d => d.FindElements(By.XPath("//h2[normalize-space()='Potential duplicate medicine']")).Count == 0);
        Assert.Equal(name, Input(browser, "name").GetAttribute("value"));

        Submit(browser);
        WaitUntil(browser, d => d.FindElement(By.XPath("//button[normalize-space()='Save duplicate anyway']"))).Click();
        Assert.Equal("Medicine added successfully", WaitUntil(browser,
            d => d.FindElement(By.XPath("//h1[normalize-space()='Medicine added successfully']"))).Text);
    });

    [Fact]
    public void TC05_Name_max_length_boundary() => RunAuthorized("US06-TC05", browser =>
    {
        var name = $"{Guid.NewGuid():N}{new string('X', 168)}";
        Assert.Equal(200, name.Length);
        FillValidForm(browser, name, "Maximum Length Generic", "Selenium Manufacturer");
        Submit(browser);

        Assert.Equal("Medicine added successfully", WaitUntil(browser,
            d => d.FindElement(By.XPath("//h1[normalize-space()='Medicine added successfully']"))).Text);
        Assert.Contains(name, browser.FindElement(By.CssSelector("main [role='status']")).Text);
    });

    [Fact]
    public void TC06_Unauthorized_user_blocked()
    {
        using var browser = CreateBrowser();
        try
        {
            Login(browser, Required("MEDZO_E2E_UNAUTHORIZED_STAFF"), Required("MEDZO_E2E_UNAUTHORIZED_PASSWORD"));
            browser.Navigate().GoToUrl($"{BaseUrl()}/catalogue/new");

            WaitUntil(browser, d => !d.Url.EndsWith("/catalogue/new", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("/catalogue/new", browser.Url, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(browser.FindElements(By.XPath("//h1[normalize-space()='Add medicine']")));
        }
        finally { SaveScreenshot(browser, "US06-TC06"); }
    }

    private static void RunAuthorized(string testCase, Action<ChromeDriver> test)
    {
        using var browser = CreateBrowser();
        try
        {
            Login(browser, Required("MEDZO_E2E_STAFF", "MEDZO_E2E_STAFF_A"),
                Required("MEDZO_E2E_PASSWORD", "MEDZO_E2E_PASSWORD_A"));
            browser.Navigate().GoToUrl($"{BaseUrl()}/catalogue/new");
            WaitUntil(browser, d => d.FindElement(By.XPath("//h1[normalize-space()='Add medicine']")));
            test(browser);
        }
        finally { SaveScreenshot(browser, testCase); }
    }

    private static void FillValidForm(IWebDriver browser, string name, string genericName,
        string manufacturer, string unitPrice = "10.50")
    {
        Set(Input(browser, "name"), name);
        Set(Input(browser, "genericName"), genericName);
        Set(Input(browser, "manufacturer"), manufacturer);
        Set(Input(browser, "unitPrice"), unitPrice);
        Set(Input(browser, "reorderThreshold"), "10");
        browser.FindElement(By.CssSelector("select[name='dosageForm']")).SendKeys("Tablet");
    }

    private static IWebElement Input(IWebDriver browser, string name) =>
        browser.FindElement(By.CssSelector($"input[name='{name}']"));

    private static void Set(IWebElement element, string value)
    {
        element.Clear();
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

            if (DateTime.UtcNow >= deadline) throw new WebDriverTimeoutException("Timed out waiting for the Add Medicine page state.");
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
