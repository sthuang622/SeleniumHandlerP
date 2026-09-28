string Param(string key, string fallback = "")
{
    return RuntimeParams.TryGetValue(key, out var value) && value != null ? value.ToString()! : fallback;
}

var selector = Param("missingSelector", "#this-element-does-not-exist");
var matches = Driver.FindElements(By.CssSelector(selector));
if (!matches.Any(element => element.Displayed))
{
    throw new Exception($"Intentional missing-element failure: selector '{selector}' was not found on {Driver.Url}.");
}

return true;
