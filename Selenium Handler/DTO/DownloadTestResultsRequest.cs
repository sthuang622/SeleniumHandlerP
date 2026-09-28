namespace Selenium_Handler.DTO {
    public class DownloadTestResultsRequest {
        public int[] FileConfigResultIds { get; set; } = Array.Empty<int>();
        public int[] TestResultIds { get; set; } = Array.Empty<int>();
    }
}
