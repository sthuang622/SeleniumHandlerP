string BaseUrl()
{
    if (Configuration == null || string.IsNullOrWhiteSpace(Configuration.Api)) throw new Exception("Configuration.Api is required.");
    return Configuration.Api.TrimEnd('/');
}

string Param(string key, string fallback = "")
{
    return RuntimeParams.TryGetValue(key, out var value) && value != null ? value.ToString()! : fallback;
}

IWebElement WaitVisible(By by, int seconds = 12)
{
    return new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds)).Until(driver =>
    {
        try
        {
            var element = driver.FindElement(by);
            return element.Displayed ? element : null;
        }
        catch (NoSuchElementException) { return null; }
        catch (StaleElementReferenceException) { return null; }
    });
}

bool IsSecureArea()
{
    try
    {
        var header = Driver.FindElements(By.CssSelector(".example h2")).FirstOrDefault(element => element.Displayed);
        return Driver.Url.Contains("/secure", StringComparison.OrdinalIgnoreCase) &&
               header != null &&
               header.Text.Contains("Secure Area", StringComparison.OrdinalIgnoreCase);
    }
    catch (StaleElementReferenceException)
    {
        return false;
    }
}

void LoginIfNeeded()
{
    if (IsSecureArea())
    {
        return;
    }

    Driver.Navigate().GoToUrl(BaseUrl() + "/login");
    WaitVisible(By.Id("username")).Clear();
    Driver.FindElement(By.Id("username")).SendKeys(Param("validUsername", "tomsmith"));
    WaitVisible(By.Id("password")).Clear();
    Driver.FindElement(By.Id("password")).SendKeys(Param("validPassword", "SuperSecretPassword!"));
    WaitVisible(By.CssSelector("button[type='submit']")).Click();
}

LoginIfNeeded();

var secure = new WebDriverWait(Driver, TimeSpan.FromSeconds(12)).Until(_ => IsSecureArea());
if (!secure)
{
    var flash = Driver.FindElements(By.Id("flash")).FirstOrDefault()?.Text?.Trim() ?? "no flash message";
    throw new Exception($"Secure area was not shown before logout. Url: {Driver.Url}. Flash: {flash}");
}

return new Dictionary<string, object?> { ["demoLoginStatus"] = "secure", ["demoSecureUrl"] = Driver.Url };
