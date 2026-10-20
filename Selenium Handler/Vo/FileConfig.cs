namespace Selenium_Handler.Vo {
    public class FileConfig {
        public string Api { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public Dictionary<string, string> Params { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public SeleniumBrowserConfig? Browser { get; init; }
        public List<FileConfigTest> Tests { get; init; } = new();
    }
}
