using Selenium_Handler.DTO;

namespace Selenium_Handler.Service.Abstraction {
    public interface ITestResultsService {
        public List<FileConfigRunResultSummaryDto> GetPastFileConfigResults(DateTime start, DateTime? end);
        public FileConfigResultDetailDto? GetPastFileConfigResultDetails(int id);
        public List<TestResultSummaryDto> GetPastTestResults(DateTime start, DateTime? end);
        public TestResultDetailDto? GetPastTestResultDetails(int id);
    }
}
