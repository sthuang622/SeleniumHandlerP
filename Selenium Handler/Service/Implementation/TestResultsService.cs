using Selenium_Handler.DTO;
using Selenium_Handler.Config;
using Selenium_Handler.Models;
using Selenium_Handler.Repo.Abstraction;
using Selenium_Handler.Service.Abstraction;

namespace Selenium_Handler.Service.Implementation {
    public class TestResultsService : ITestResultsService {
        private readonly ITestResultRepo _testResultRepo;
        private readonly FolderPathResolver _folderPathResolver;

        public TestResultsService(ITestResultRepo testResultRepo, FolderPathResolver folderPathResolver) {
            _testResultRepo = testResultRepo;
            _folderPathResolver = folderPathResolver;
        }

        public List<FileConfigRunResultSummaryDto> GetPastFileConfigResults(DateTime start, DateTime? end) {
            return _testResultRepo
                .GetPastFileConfigResults(start, end)
                .Select(MapFileConfigResultSummary)
                .GroupBy(fileConfigResult => fileConfigResult.RunId)
                .Select(runGroup => {
                    var fileConfigResults = runGroup
                        .OrderBy(fileConfigResult => fileConfigResult.DoneDateTime)
                        .ThenBy(fileConfigResult => fileConfigResult.FileConfigResultId)
                        .ToList();

                    return new FileConfigRunResultSummaryDto {
                        RunId = runGroup.Key,
                        RunResult = GetRunResultLabel(fileConfigResults),
                        FileConfigCount = fileConfigResults.Count,
                        TestCount = fileConfigResults.Sum(fileConfigResult => fileConfigResult.TestCount),
                        TestSuccessCount = fileConfigResults.Sum(fileConfigResult => fileConfigResult.TestSuccessCount),
                        TestFailCount = fileConfigResults.Sum(fileConfigResult => fileConfigResult.TestFailCount),
                        StepSuccessCount = fileConfigResults.Sum(fileConfigResult => fileConfigResult.StepSuccessCount),
                        StepFailCount = fileConfigResults.Sum(fileConfigResult => fileConfigResult.StepFailCount),
                        DoneDateTime = fileConfigResults.Max(fileConfigResult => fileConfigResult.DoneDateTime),
                        FileConfigResults = fileConfigResults
                    };
                })
                .OrderByDescending(runResult => runResult.DoneDateTime)
                .ToList();
        }

        private static FileConfigResultSummaryDto MapFileConfigResultSummary(FileConfigResult fileConfigResult) {
            return new FileConfigResultSummaryDto {
                FileConfigResultId = fileConfigResult.Id,
                RunId = fileConfigResult.RunId,
                FileConfigName = fileConfigResult.Name,
                FileConfigResult = GetFileConfigResultLabel(fileConfigResult.TestResults),
                TestCount = fileConfigResult.TestResults.Count,
                TestSuccessCount = fileConfigResult.TestResults.Count(testResult =>
                    string.Equals(GetTestResultLabel(testResult.StepResults), "Success", StringComparison.OrdinalIgnoreCase)),
                TestFailCount = fileConfigResult.TestResults.Count(testResult =>
                    string.Equals(GetTestResultLabel(testResult.StepResults), "Failure", StringComparison.OrdinalIgnoreCase)),
                StepSuccessCount = fileConfigResult.TestResults.Sum(testResult => testResult.StepResults.Count(stepResult =>
                    IsSuccessResult(stepResult.Result))),
                StepFailCount = fileConfigResult.TestResults.Sum(testResult => testResult.StepResults.Count(stepResult =>
                    IsFailureResult(stepResult.Result))),
                DoneDateTime = fileConfigResult.CreatedAt
            };
        }

        public FileConfigResultDetailDto? GetPastFileConfigResultDetails(int id) {
            var fileConfigResult = _testResultRepo.GetPastFileConfigResultDetails(id);

            if (fileConfigResult is null) {
                return null;
            }

            return new FileConfigResultDetailDto {
                FileConfigResultId = fileConfigResult.Id,
                RunId = fileConfigResult.RunId,
                Name = fileConfigResult.Name,
                Description = fileConfigResult.Description,
                FilePath = fileConfigResult.FilePath,
                DoneDateTime = fileConfigResult.CreatedAt,
                TestConfigs = fileConfigResult.TestResults
                    .OrderBy(testResult => testResult.Step)
                    .ThenBy(testResult => testResult.Id)
                    .Select(MapTestResultDetail)
                    .ToList()
            };
        }

        public List<TestResultSummaryDto> GetPastTestResults(DateTime start, DateTime? end) {
            return _testResultRepo
                .GetPastTestResults(start, end)
                .Select(testResult => new TestResultSummaryDto {
                    TestId = testResult.Id,
                    Step = testResult.Step,
                    TestName = testResult.Name,
                    TestResult = GetTestResultLabel(testResult.StepResults),
                    TestSuccessCount = testResult.StepResults.Count(stepResult =>
                        IsSuccessResult(stepResult.Result)),
                    TestFailCount = testResult.StepResults.Count(stepResult =>
                        IsFailureResult(stepResult.Result)),
                    TestDoneDateTime = testResult.CreatedAt,
                    UpdateTime = testResult.UpdateTime
                })
                .ToList();
        }

        public TestResultDetailDto? GetPastTestResultDetails(int id) {
            var testResult = _testResultRepo.GetPastTestResultDetails(id);

            if (testResult is null) {
                return null;
            }

            return MapTestResultDetail(testResult);
        }

        private TestResultDetailDto MapTestResultDetail(TestResult testResult) {
            return new TestResultDetailDto {
                TestId = testResult.Id,
                Step = testResult.Step,
                Name = testResult.Name,
                Description = testResult.Description,
                FilePath = testResult.FilePath,
                TestDoneDateTime = testResult.CreatedAt,
                UpdateTime = testResult.UpdateTime,
                StepResults = testResult.StepResults
                    .OrderBy(stepResult => stepResult.Step)
                    .ThenBy(stepResult => stepResult.Id)
                    .Select(stepResult => new StepResultDetailDto {
                        StepResultId = stepResult.Id,
                        Step = stepResult.Step,
                        Name = stepResult.Name,
                        Description = stepResult.Description,
                        Screenshot = _folderPathResolver.ToScreenshotUrl(stepResult.Screenshot),
                        Result = stepResult.Result,
                        ErrorMessage = stepResult.ErrorMessage,
                        CreatedAt = stepResult.CreatedAt,
                        UpdateTime = stepResult.UpdateTime
                    })
                    .ToList()
            };
        }

        private static string GetRunResultLabel(IReadOnlyCollection<FileConfigResultSummaryDto> fileConfigResults) {
            if (fileConfigResults.Count == 0) {
                return "Not Run";
            }

            if (fileConfigResults.Any(fileConfigResult =>
                    string.Equals(fileConfigResult.FileConfigResult, "Failure", StringComparison.OrdinalIgnoreCase))) {
                return "Failure";
            }

            if (fileConfigResults.Any(fileConfigResult =>
                    string.Equals(fileConfigResult.FileConfigResult, "Not Run", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileConfigResult.FileConfigResult, "Skipped", StringComparison.OrdinalIgnoreCase))) {
                return "Not Run";
            }

            return "Success";
        }

        private static string GetFileConfigResultLabel(IReadOnlyCollection<TestResult> testResults) {
            if (testResults.Count == 0) {
                return "Not Run";
            }

            var labels = testResults.Select(testResult => GetTestResultLabel(testResult.StepResults)).ToList();

            if (labels.Any(label => string.Equals(label, "Failure", StringComparison.OrdinalIgnoreCase))) {
                return "Failure";
            }

            if (labels.Any(label => string.Equals(label, "Not Run", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(label, "Skipped", StringComparison.OrdinalIgnoreCase))) {
                return "Not Run";
            }

            return "Success";
        }

        private static string GetTestResultLabel(IReadOnlyCollection<StepResult> stepResults) {
            if (stepResults.Count == 0 ||
                stepResults.All(stepResult => IsNotRunResult(stepResult.Result))) {
                return "Not Run";
            }

            if (stepResults.Any(stepResult => IsFailureResult(stepResult.Result))) {
                return "Failure";
            }

            if (stepResults.All(stepResult => IsSkippedResult(stepResult.Result))) {
                return "Skipped";
            }

            if (stepResults.Any(stepResult => IsNotRunResult(stepResult.Result) || IsSkippedResult(stepResult.Result))) {
                return "Not Run";
            }

            return "Success";
        }

        private static bool IsSuccessResult(string result) {
            return string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(result, "Passed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFailureResult(string result) {
            return string.Equals(result, "Failed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(result, "Error", StringComparison.OrdinalIgnoreCase) ||
                   result.Contains("failed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNotRunResult(string result) {
            return string.Equals(result, "Not Run", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSkippedResult(string result) {
            return string.Equals(result, "Skipped", StringComparison.OrdinalIgnoreCase);
        }
    }
}
