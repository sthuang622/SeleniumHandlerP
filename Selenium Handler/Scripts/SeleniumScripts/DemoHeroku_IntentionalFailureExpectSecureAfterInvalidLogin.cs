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

var flash = Driver.FindElements(By.Id("flash")).FirstOrDefault(element => element.Displayed)?.Text?.Trim() ?? "no visible flash message";

if (!IsSecureArea())
{
    throw new Exception($"Intentional demo failure: invalid credentials should not reach the secure page. This failure is expected and exists to demonstrate failed-result reporting. Current URL: {Driver.Url}. Message: {flash}");
}

throw new Exception("Intentional demo failure did not behave as expected because the invalid login reached the secure page.");
