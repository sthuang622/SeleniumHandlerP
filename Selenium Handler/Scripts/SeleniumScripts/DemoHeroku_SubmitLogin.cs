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
bool WaitGone(By by, int seconds = 12)
{
    return new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds)).Until(driver =>
    {
        try { return !driver.FindElement(by).Displayed; }
        catch (NoSuchElementException) { return true; }
        catch (StaleElementReferenceException) { return true; }
    });
}
void Go(string path)
{
    Driver.Navigate().GoToUrl(BaseUrl() + path);
}
var submitButton = WaitVisible(By.CssSelector("button[type=submit]"));
submitButton.Click();

return new WebDriverWait(Driver, TimeSpan.FromSeconds(12)).Until(driver =>
{
    try
    {
        var flashElements = driver.FindElements(By.Id("flash"));
        if (flashElements.Any(element => element.Displayed && !string.IsNullOrWhiteSpace(element.Text)))
        {
            return true;
        }

        var secureHeaders = driver.FindElements(By.CssSelector(".example h2"));
        if (driver.Url.Contains("/secure") && secureHeaders.Any(element => element.Displayed && element.Text.Contains("Secure Area")))
        {
            return true;
        }

        return false;
    }
    catch (StaleElementReferenceException)
    {
        return false;
    }
});
