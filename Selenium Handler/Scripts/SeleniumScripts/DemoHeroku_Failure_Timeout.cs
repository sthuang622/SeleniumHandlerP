string Param(string key, string fallback = "")
{
    return RuntimeParams.TryGetValue(key, out var value) && value != null ? value.ToString()! : fallback;
}

var selector = Param("timeoutSelector", "#never-appears");
try
{
    new WebDriverWait(Driver, TimeSpan.FromSeconds(5)).Until(driver =>
    {
        try
        {
            var element = driver.FindElement(By.CssSelector(selector));
            return element.Displayed ? element : null;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    });
}
catch (WebDriverTimeoutException)
{
    throw new Exception($"Intentional timeout failure: selector '{selector}' did not appear within 5 seconds on {Driver.Url}.");
}

return true;
