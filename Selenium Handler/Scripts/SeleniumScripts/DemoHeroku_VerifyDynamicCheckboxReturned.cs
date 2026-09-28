bool WaitUntil(Func<bool> condition, int seconds = 20)
{
    return new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds)).Until(_ =>
    {
        try { return condition(); }
        catch (NoSuchElementException) { return false; }
        catch (StaleElementReferenceException) { return false; }
    });
}

var returned = WaitUntil(() =>
{
    var checkboxVisible = Driver
        .FindElements(By.CssSelector("#checkbox input[type='checkbox'], #checkbox-example input[type='checkbox']"))
        .Any(checkbox => checkbox.Displayed);

    if (checkboxVisible)
    {
        return true;
    }

    return Driver
        .FindElements(By.Id("message"))
        .Any(message => message.Displayed && message.Text.Contains("back", StringComparison.OrdinalIgnoreCase));
});

if (!returned)
{
    var messageText = Driver.FindElements(By.Id("message")).FirstOrDefault()?.Text?.Trim() ?? "no message";
    throw new Exception($"Dynamic checkbox did not return. Current message: {messageText}");
}

return true;
