namespace Selenium_Handler.DTO {
    public class FileConfigResultDetailDto {
        public int FileConfigResultId { get; set; }
        public Guid RunId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime DoneDateTime { get; set; }
        public List<TestResultDetailDto> TestConfigs { get; set; } = new();
    }
}
