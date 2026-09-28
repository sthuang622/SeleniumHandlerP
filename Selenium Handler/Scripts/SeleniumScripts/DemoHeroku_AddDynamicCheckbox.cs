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
var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
var addButton = wait.Until(driver =>
{
    try
    {
        return driver
            .FindElements(By.CssSelector("#checkbox-example button, button"))
            .FirstOrDefault(button =>
                button.Displayed &&
                button.Enabled &&
                string.Equals(button.Text.Trim(), "Add", StringComparison.OrdinalIgnoreCase));
    }
    catch (StaleElementReferenceException)
    {
        return null;
    }
});

if (addButton == null)
{
    return false;
}

((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({ block: 'center' });", addButton);

try
{
    addButton.Click();
}
catch (ElementClickInterceptedException)
{
    ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", addButton);
}

return wait.Until(driver =>
{
    try
    {
        var checkboxBack = driver
            .FindElements(By.CssSelector("#checkbox input[type=checkbox], #checkbox-example input[type=checkbox]"))
            .Any(checkbox => checkbox.Displayed);
        var messageBack = driver
            .FindElements(By.Id("message"))
            .Any(message => message.Displayed && message.Text.Contains("back", StringComparison.OrdinalIgnoreCase));

        return checkboxBack || messageBack;
    }
    catch (StaleElementReferenceException)
    {
        return false;
    }
});
