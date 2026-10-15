namespace Selenium_Handler.Vo {
    public class SeleniumChromeDriverConfig {
        public List<string> Arguments { get; init; } = new();
        public Dictionary<string, object?> UserProfilePreferences { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
