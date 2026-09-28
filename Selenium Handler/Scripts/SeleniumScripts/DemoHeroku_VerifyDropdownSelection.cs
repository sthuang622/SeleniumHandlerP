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

var desiredValue = Param("dropdownValue", "2").Trim();
var dropdown = WaitVisible(By.Id("dropdown"));
var selectedValue = ((IJavaScriptExecutor)Driver).ExecuteScript("return arguments[0].value || '';", dropdown)?.ToString()?.Trim() ?? "";
var selectedText = ((IJavaScriptExecutor)Driver).ExecuteScript("return arguments[0].selectedIndex >= 0 ? arguments[0].options[arguments[0].selectedIndex].text : '';", dropdown)?.ToString()?.Trim() ?? "";

if (!string.Equals(selectedValue, desiredValue, StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(selectedText, desiredValue, StringComparison.OrdinalIgnoreCase))
{
    throw new Exception($"Dropdown selected '{selectedText}' with value '{selectedValue}', expected '{desiredValue}'.");
}

return new Dictionary<string, object?> { ["selectedDropdownText"] = selectedText, ["selectedDropdownValue"] = selectedValue };
