namespace Selenium_Handler.DTO {
    public class TestResultDetailDto {
        public int TestId { get; set; }
        public int Step { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime TestDoneDateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<StepResultDetailDto> StepResults { get; set; } = new();
    }
}
