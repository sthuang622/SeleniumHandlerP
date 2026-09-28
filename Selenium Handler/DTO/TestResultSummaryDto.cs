namespace Selenium_Handler.DTO {
    public class TestResultSummaryDto {
        public int TestId { get; set; }
        public int Step { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string TestResult { get; set; } = string.Empty;
        public int TestSuccessCount { get; set; }
        public int TestFailCount { get; set; }
        public DateTime TestDoneDateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
