namespace Selenium_Handler.DTO {
    public class FileConfigResultSummaryDto {
        public int FileConfigResultId { get; set; }
        public Guid RunId { get; set; }
        public string FileConfigName { get; set; } = string.Empty;
        public string FileConfigResult { get; set; } = string.Empty;
        public int TestCount { get; set; }
        public int TestSuccessCount { get; set; }
        public int TestFailCount { get; set; }
        public int StepSuccessCount { get; set; }
        public int StepFailCount { get; set; }
        public DateTime DoneDateTime { get; set; }
    }
}
