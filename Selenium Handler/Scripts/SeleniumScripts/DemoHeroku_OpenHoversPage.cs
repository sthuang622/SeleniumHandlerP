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

Go("/hovers");
return WaitVisible(By.CssSelector(".figure")) != null;
