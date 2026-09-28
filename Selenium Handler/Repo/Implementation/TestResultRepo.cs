using Microsoft.EntityFrameworkCore;
using Selenium_Handler.Models;
using Selenium_Handler.Repo.Abstraction;

namespace Selenium_Handler.Repo.Implementation {
    public class TestResultRepo : ITestResultRepo {
        private readonly AppDbContext _dbContext;

        public TestResultRepo(AppDbContext dbContext) {
            _dbContext = dbContext;
        }

        public List<FileConfigResult> GetPastFileConfigResults(DateTime start, DateTime? end) {
            var query = _dbContext.FileConfigResults
                .AsNoTracking()
                .Include(fileConfigResult => fileConfigResult.TestResults)
                    .ThenInclude(testResult => testResult.StepResults)
                .Where(fileConfigResult => fileConfigResult.CreatedAt >= start);

            if (end is not null) {
                query = query.Where(fileConfigResult => fileConfigResult.CreatedAt < end.Value);
            }

            return query
                .OrderByDescending(fileConfigResult => fileConfigResult.CreatedAt)
                .ToList();
        }

        public FileConfigResult? GetPastFileConfigResultDetails(int id) {
            return _dbContext.FileConfigResults
                .AsNoTracking()
                .Include(fileConfigResult => fileConfigResult.TestResults)
                    .ThenInclude(testResult => testResult.StepResults)
                .FirstOrDefault(fileConfigResult => fileConfigResult.Id == id);
        }

        public List<TestResult> GetTestResultsByFileConfigResultIds(IEnumerable<int> ids) {
            var idList = ids.Distinct().ToList();

            if (idList.Count == 0) {
                return new List<TestResult>();
            }

            return _dbContext.TestResults
                .AsNoTracking()
                .Include(testResult => testResult.FileConfigResult)
                .Include(testResult => testResult.StepResults)
                .Where(testResult => testResult.FileConfigResultId != null && idList.Contains(testResult.FileConfigResultId.Value))
                .OrderBy(testResult => testResult.FileConfigResultId)
                .ThenBy(testResult => testResult.Step)
                .ThenBy(testResult => testResult.Id)
                .ToList();
        }

        public List<TestResult> GetPastTestResults(DateTime start, DateTime? end) {
            var query = _dbContext.TestResults
                .AsNoTracking()
                .Include(testResult => testResult.FileConfigResult)
                .Include(testResult => testResult.StepResults)
                .Where(testResult => testResult.CreatedAt >= start);

            if (end is not null) {
                query = query.Where(testResult => testResult.CreatedAt < end.Value);
            }

            return query
                .OrderByDescending(testResult => testResult.CreatedAt)
                .ThenBy(testResult => testResult.Step)
                .ThenBy(testResult => testResult.Id)
                .ToList();
        }

        public List<TestResult> GetTestResultsByIds(IEnumerable<int> ids) {
            var idList = ids.Distinct().ToList();

            if (idList.Count == 0) {
                return new List<TestResult>();
            }

            return _dbContext.TestResults
                .AsNoTracking()
                .Include(testResult => testResult.FileConfigResult)
                .Include(testResult => testResult.StepResults)
                .Where(testResult => idList.Contains(testResult.Id))
                .OrderBy(testResult => testResult.FileConfigResultId)
                .ThenBy(testResult => testResult.Step)
                .ThenBy(testResult => testResult.Id)
                .ToList();
        }

        public TestResult? GetPastTestResultDetails(int id) {
            return _dbContext.TestResults
                .AsNoTracking()
                .Include(testResult => testResult.FileConfigResult)
                .Include(testResult => testResult.StepResults)
                .FirstOrDefault(testResult => testResult.Id == id);
        }

        public FileConfigResult CreateFileConfigResult(FileConfigResult fileConfigResult) {
            _dbContext.FileConfigResults.Add(fileConfigResult);
            _dbContext.SaveChanges();
            return fileConfigResult;
        }

        public TestResult CreateTestResult(TestResult testResult) {
            _dbContext.TestResults.Add(testResult);
            _dbContext.SaveChanges();
            return testResult;
        }

        public void UpdateStepResult(StepResult stepResult) {
            var updateTime = DateTime.UtcNow;
            stepResult.UpdateTime = updateTime;

            if (stepResult.TestResult is not null) {
                stepResult.TestResult.UpdateTime = updateTime;
            }

            _dbContext.StepResults.Update(stepResult);
            _dbContext.SaveChanges();

            _dbContext.Database.ExecuteSqlInterpolated(
                $"UPDATE dbo.TestResult SET UpdateTime = {updateTime} WHERE Id = {stepResult.TestResultId}");
        }
    }
}
