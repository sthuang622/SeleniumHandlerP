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

bool WaitUntil(Func<bool> condition, int seconds = 12)
{
    return new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds)).Until(_ =>
    {
        try { return condition(); }
        catch (StaleElementReferenceException) { return false; }
        catch (NoSuchElementException) { return false; }
    });
}

string SelectedValue()
{
    return ((IJavaScriptExecutor)Driver).ExecuteScript("return document.getElementById('dropdown')?.value || '';")?.ToString()?.Trim() ?? "";
}

string SelectedText()
{
    return ((IJavaScriptExecutor)Driver).ExecuteScript("const s = document.getElementById('dropdown'); return s && s.selectedIndex >= 0 ? s.options[s.selectedIndex].text : '';")?.ToString()?.Trim() ?? "";
}

var desiredValue = Param("dropdownValue", "2").Trim();
var dropdown = WaitVisible(By.Id("dropdown"));
var options = dropdown.FindElements(By.TagName("option"));
var option = options.FirstOrDefault(option =>
    string.Equals((option.GetAttribute("value") ?? "").Trim(), desiredValue, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(option.Text.Trim(), desiredValue, StringComparison.OrdinalIgnoreCase));

if (option == null)
{
    var available = string.Join(", ", options.Select(option => $"{option.Text.Trim()}={option.GetAttribute("value")}"));
    throw new Exception($"Dropdown option '{desiredValue}' was not found. Available options: {available}");
}

var targetValue = (option.GetAttribute("value") ?? desiredValue).Trim();
var targetText = option.Text.Trim();

try
{
    option.Click();
}
catch
{
    ((IJavaScriptExecutor)Driver).ExecuteScript(
        "const select = arguments[0]; select.value = arguments[1]; select.dispatchEvent(new Event('input', { bubbles: true })); select.dispatchEvent(new Event('change', { bubbles: true }));",
        dropdown,
        targetValue);
}

var selected = WaitUntil(() =>
    string.Equals(SelectedValue(), targetValue, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(SelectedText(), desiredValue, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(SelectedText(), targetText, StringComparison.OrdinalIgnoreCase));

if (!selected)
{
    throw new Exception($"Dropdown stayed on '{SelectedText()}' with value '{SelectedValue()}', expected '{desiredValue}'.");
}

return true;
