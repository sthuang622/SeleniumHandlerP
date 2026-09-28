string Param(string key, string fallback = "")
{
    return RuntimeParams.TryGetValue(key, out var value) && value != null ? value.ToString()! : fallback;
}

var expected = Param("wrongExpectedText", "This text should not exist");
var bodyText = Driver.FindElement(By.TagName("body")).Text;
if (!bodyText.Contains(expected, StringComparison.OrdinalIgnoreCase))
{
    throw new Exception($"Intentional wrong-text failure: expected page text '{expected}' was not found on {Driver.Url}.");
}

return true;
