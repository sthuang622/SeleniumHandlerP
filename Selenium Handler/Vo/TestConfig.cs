namespace Selenium_Handler.Vo {
    public class TestConfig {
        public string Api { get; set; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public Dictionary<string, string> Params { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public SeleniumChromeDriverConfig? ChromeDriver { get; init; }
        public SeleniumScriptOptionsConfig? ScriptOptions { get; init; }
        public List<TestStep> TestSteps { get; init; } = new();
    }
}


