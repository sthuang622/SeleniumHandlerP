namespace Selenium_Handler.DTO {
    public class StepResultDetailDto {
        public int StepResultId { get; set; }
        public int Step { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Screenshot { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
