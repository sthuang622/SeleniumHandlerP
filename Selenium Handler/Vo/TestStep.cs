namespace Selenium_Handler.Vo {
    public class TestStep {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Dependency { get; init; }
        public string SeleniumScript { get; init; } = string.Empty;
    }
}