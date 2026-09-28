using Selenium_Handler.Models;

namespace Selenium_Handler.Repo.Abstraction {
    public interface ITestResultRepo {
        public List<FileConfigResult> GetPastFileConfigResults(DateTime start, DateTime? end);
        public FileConfigResult? GetPastFileConfigResultDetails(int id);
        public List<TestResult> GetTestResultsByFileConfigResultIds(IEnumerable<int> ids);
        public List<TestResult> GetPastTestResults(DateTime start, DateTime? end);
        public List<TestResult> GetTestResultsByIds(IEnumerable<int> ids);
        public TestResult? GetPastTestResultDetails(int id);
        public FileConfigResult CreateFileConfigResult(FileConfigResult fileConfigResult);
        public TestResult CreateTestResult(TestResult testResult);
        public void UpdateStepResult(StepResult stepResult);
    }
}
