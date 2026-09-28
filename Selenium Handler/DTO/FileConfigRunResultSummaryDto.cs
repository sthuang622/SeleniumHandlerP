namespace Selenium_Handler.DTO {
    public class FileConfigRunResultSummaryDto {
        public Guid RunId { get; set; }
        public string RunResult { get; set; } = string.Empty;
        public int FileConfigCount { get; set; }
        public int TestCount { get; set; }
        public int TestSuccessCount { get; set; }
        public int TestFailCount { get; set; }
        public int StepSuccessCount { get; set; }
        public int StepFailCount { get; set; }
        public DateTime DoneDateTime { get; set; }
        public List<FileConfigResultSummaryDto> FileConfigResults { get; set; } = new();
    }
}
