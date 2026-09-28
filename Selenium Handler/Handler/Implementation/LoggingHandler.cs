using Microsoft.Extensions.Options;
using Selenium_Handler.Config;
using Selenium_Handler.Handler.Abstraction;
using Selenium_Handler.Models;
using Selenium_Handler.Repo.Abstraction;
using System.IO.Compression;
using System.Text;

namespace Selenium_Handler.Handler.Implementation {
    public class LoggingHandler : ILoggingHandler {
        private readonly IWebHostEnvironment _environment;
        private readonly FolderPaths _folderPaths;
        private readonly FolderPathResolver _folderPathResolver;
        private readonly ITestResultRepo _testResultRepo;

        public LoggingHandler(
            IWebHostEnvironment environment,
            IOptions<FolderPaths> folderPaths,
            FolderPathResolver folderPathResolver,
            ITestResultRepo testResultRepo) {
            _environment = environment;
            _folderPaths = folderPaths.Value;
            _folderPathResolver = folderPathResolver;
            _testResultRepo = testResultRepo;
        }

        public string ReturnResult() {
            return ReturnResult(string.Empty);
        }

        public string ReturnResult(string requestBaseUrl) {
            var testResults = GetAllTestResults();
            return WriteResultReport(testResults, requestBaseUrl);
        }

        public string ReturnResult(IEnumerable<int> testResultIds) {
            return ReturnResult(testResultIds, string.Empty);
        }

        public string ReturnResult(IEnumerable<int> testResultIds, string requestBaseUrl) {
            var testResults = _testResultRepo.GetTestResultsByIds(testResultIds);
            return WriteResultReport(testResults, requestBaseUrl);
        }

        public string ReturnResultPackage() {
            return WriteResultPackage(GetAllTestResults());
        }

        public string ReturnResultPackage(IEnumerable<int> testResultIds) {
            return WriteResultPackage(_testResultRepo.GetTestResultsByIds(testResultIds));
        }

        public string ReturnResultPackageForFileConfigResults(IEnumerable<int> fileConfigResultIds) {
            return WriteResultPackage(_testResultRepo.GetTestResultsByFileConfigResultIds(fileConfigResultIds));
        }

        public string TakeSnapShot() {
            var screenshotsFolder = EnsureFolder(_folderPaths.ScreenshotFolder);
            return Path.Combine(screenshotsFolder, $"{DateTime.Now:yyyy-MM-dd_HHmmss}_snapshot.png");
        }

        private IReadOnlyCollection<TestResult> GetAllTestResults() {
            return _testResultRepo
                .GetPastTestResults(DateTime.MinValue, null)
                .OrderByDescending(testResult => testResult.CreatedAt)
                .ThenBy(testResult => testResult.Step)
                .ThenBy(testResult => testResult.Id)
                .ToList();
        }

        private string WriteResultReport(IReadOnlyCollection<TestResult> testResults, string requestBaseUrl) {
            var logsFolder = EnsureFolder(_folderPaths.LogFolder);
            var filePath = Path.Combine(logsFolder, $"{DateTime.Now:yyyy-MM-dd_HHmmss}_result.html");

            File.WriteAllText(
                filePath,
                ResultReportFactory.BuildResultReport(testResults, screenshotPath => BuildWebScreenshotUrl(screenshotPath, requestBaseUrl)),
                Encoding.UTF8);

            return filePath;
        }

        private string WriteResultPackage(IReadOnlyCollection<TestResult> testResults) {
            var logsFolder = EnsureFolder(_folderPaths.LogFolder);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var packageFolder = Path.Combine(logsFolder, $"{timestamp}_result_package");
            var packageScreenshotsFolder = Path.Combine(packageFolder, "screenshots");
            var zipPath = Path.Combine(logsFolder, $"{timestamp}_result_package.zip");

            if (Directory.Exists(packageFolder)) {
                Directory.Delete(packageFolder, true);
            }

            if (File.Exists(zipPath)) {
                File.Delete(zipPath);
            }

            Directory.CreateDirectory(packageScreenshotsFolder);
            var screenshotMap = CopyScreenshotsToPackage(testResults, packageScreenshotsFolder);

            var reportPath = Path.Combine(packageFolder, "index.html");
            File.WriteAllText(
                reportPath,
                ResultReportFactory.BuildResultReport(testResults, screenshotPath => BuildPackagedScreenshotUrl(screenshotPath, screenshotMap)),
                Encoding.UTF8);

            ZipFile.CreateFromDirectory(packageFolder, zipPath, CompressionLevel.Fastest, false);

            return zipPath;
        }

        private string EnsureFolder(string folderName) {
            var folderPath = ResolvePath(folderName);
            Directory.CreateDirectory(folderPath);
            return folderPath;
        }

        private string ResolvePath(string path) {
            if (Path.IsPathRooted(path)) {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(ResolveBasePath(), path));
        }

        private string ResolveBasePath() {
            return Path.GetFullPath(Path.IsPathRooted(_folderPaths.BasePath)
                ? _folderPaths.BasePath
                : Path.Combine(_environment.ContentRootPath, _folderPaths.BasePath));
        }

        private static Dictionary<string, string> CopyScreenshotsToPackage(IReadOnlyCollection<TestResult> testResults, string packageScreenshotsFolder) {
            var screenshotMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var screenshotPaths = testResults
                .SelectMany(testResult => testResult.StepResults)
                .Select(stepResult => stepResult.Screenshot)
                .Where(screenshotPath => !string.IsNullOrWhiteSpace(screenshotPath))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var screenshotPath in screenshotPaths) {
                var resolvedScreenshotPath = ResolveScreenshotFilePath(screenshotPath!);

                if (string.IsNullOrWhiteSpace(resolvedScreenshotPath) || !File.Exists(resolvedScreenshotPath)) {
                    continue;
                }

                var fileName = Path.GetFileName(resolvedScreenshotPath);

                if (string.IsNullOrWhiteSpace(fileName)) {
                    continue;
                }

                var uniqueFileName = GetUniqueFileName(fileName, usedFileNames);
                var destinationPath = Path.Combine(packageScreenshotsFolder, uniqueFileName);
                File.Copy(resolvedScreenshotPath, destinationPath, false);

                var packageScreenshotUrl = $"screenshots/{Uri.EscapeDataString(uniqueFileName).Replace("%2F", "/")}";
                screenshotMap[screenshotPath!] = packageScreenshotUrl;
                screenshotMap[resolvedScreenshotPath] = packageScreenshotUrl;
            }

            return screenshotMap;
        }

        private static string? ResolveScreenshotFilePath(string screenshotPath) {
            if (string.IsNullOrWhiteSpace(screenshotPath)) {
                return null;
            }

            if (Uri.TryCreate(screenshotPath, UriKind.Absolute, out var screenshotUri)) {
                if (screenshotUri.IsFile) {
                    return screenshotUri.LocalPath;
                }

                return null;
            }

            return Path.GetFullPath(screenshotPath);
        }

        private static string GetUniqueFileName(string fileName, HashSet<string> usedFileNames) {
            if (usedFileNames.Add(fileName)) {
                return fileName;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var count = 1;
            string uniqueFileName;

            do {
                uniqueFileName = $"{fileNameWithoutExtension}_{count}{extension}";
                count++;
            } while (!usedFileNames.Add(uniqueFileName));

            return uniqueFileName;
        }

        private static string? BuildPackagedScreenshotUrl(string? screenshotPath, IReadOnlyDictionary<string, string> screenshotMap) {
            if (string.IsNullOrWhiteSpace(screenshotPath)) {
                return null;
            }

            if (screenshotMap.TryGetValue(screenshotPath, out var screenshotUrl)) {
                return screenshotUrl;
            }

            var resolvedScreenshotPath = ResolveScreenshotFilePath(screenshotPath);

            if (!string.IsNullOrWhiteSpace(resolvedScreenshotPath) && screenshotMap.TryGetValue(resolvedScreenshotPath, out screenshotUrl)) {
                return screenshotUrl;
            }

            return null;
        }

        private string? BuildWebScreenshotUrl(string? screenshotPath, string requestBaseUrl) {
            var screenshotUrl = _folderPathResolver.ToScreenshotUrl(screenshotPath);

            if (string.IsNullOrWhiteSpace(screenshotUrl)) {
                return null;
            }

            return ToAbsoluteUrl(screenshotUrl, requestBaseUrl);
        }

        private static string ToAbsoluteUrl(string url, string requestBaseUrl) {
            if (Uri.TryCreate(url, UriKind.Absolute, out _)) {
                return url;
            }

            if (string.IsNullOrWhiteSpace(requestBaseUrl)) {
                return url;
            }

            return $"{requestBaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
        }

    }
}
