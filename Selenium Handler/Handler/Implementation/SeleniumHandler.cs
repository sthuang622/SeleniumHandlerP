using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Microsoft.Extensions.Options;
using Selenium_Handler.Config;
using Selenium_Handler.Handler.Abstraction;
using Selenium_Handler.Models;
using Selenium_Handler.Repo.Abstraction;
using Selenium_Handler.Vo;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections;

namespace Selenium_Handler.Handler.Implementation {
    public class SeleniumHandler : ISeleniumHandler {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SeleniumHandler> _logger;
        private readonly FolderPaths _folderPaths;
        private readonly ITestResultRepo _testResultRepo;
        private readonly Dictionary<string, object?> _sharedData = new(StringComparer.OrdinalIgnoreCase);
        private IWebDriver? _driver;
        private TestConfig? _currentConfig;
        private TestStep? _currentStep;

        public SeleniumHandler(
            IWebHostEnvironment environment,
            ILogger<SeleniumHandler> logger,
            IOptions<FolderPaths> folderPaths,
            ITestResultRepo testResultRepo) {
            _environment = environment;
            _logger = logger;
            _folderPaths = folderPaths.Value;
            _testResultRepo = testResultRepo;
        }

        public string GetFileConfigPath(string testName) {
            if (string.IsNullOrWhiteSpace(testName)) {
                throw new ArgumentException("A config name or path is required.", nameof(testName));
            }

            if (Path.IsPathRooted(testName)) {
                if (!File.Exists(testName)) {
                    throw new FileNotFoundException("The file config was not found.", testName);
                }

                return Path.GetFullPath(testName);
            }

            var directPath = ResolvePath(testName);

            if (File.Exists(directPath)) {
                return directPath;
            }

            var fileName = Path.HasExtension(testName) ? testName : $"{testName}.json";
            var configFolder = ResolvePath(_folderPaths.FileConfigFolder);
            var configPath = Path.GetFullPath(Path.Combine(configFolder, fileName));

            if (!File.Exists(configPath)) {
                throw new FileNotFoundException("The file config was not found.", configPath);
            }

            return configPath;
        }

        public void Run(string fileConfigPath, Guid runId) {
            var configPath = GetFileConfigPath(fileConfigPath);
            RunConfig(configPath, new HashSet<string>(StringComparer.OrdinalIgnoreCase), null, runId);
        }

        private void RunConfig(string configPath, HashSet<string> configStack, int? fileConfigResultId, Guid runId) {
            configPath = Path.GetFullPath(configPath);

            if (!configStack.Add(configPath)) {
                throw new InvalidOperationException($"File config cycle detected at '{configPath}'.");
            }

            try {
                var fileConfig = LoadConfig(configPath);

                if (fileConfig.Tests.Count > 0) {
                    ValidateWrapperConfig(fileConfig);
                    LoadConfigParams(fileConfig);
                    var fileConfigResult = CreateFileConfigResult(configPath, fileConfig, runId);
                    _logger.LogInformation("START FILECONFIG {ConfigPath}", configPath);

                    var testRuns = CreatePendingTestRuns(fileConfig.Tests, configPath, fileConfigResult.Id, fileConfig.Name, fileConfig.Api);
                    var testOutcomes = new Dictionary<string, TestConfigOutcome>(StringComparer.OrdinalIgnoreCase);

                    EnsureBrowser();

                    foreach (var testRun in testRuns) {
                        var testName = GetTestConfigName(testRun.ConfigPath, testRun.Config);

                        if (HasFailedTestDependency(testRun, testOutcomes, out var dependencyName)) {
                            MarkTestResultSkipped(testRun.TestResult, dependencyName);
                            testOutcomes[testName] = TestConfigOutcome.Skipped;
                            continue;
                        }

                        var passed = RunExecutableConfig(testRun.ConfigPath, testRun.Config, testRun.TestResult);
                        testOutcomes[testName] = passed ? TestConfigOutcome.Success : TestConfigOutcome.Failure;
                    }

                    _logger.LogInformation("END FILECONFIG {ConfigPath}", configPath);
                    return;
                }

                var testConfig = LoadTestConfig(configPath);

                if (fileConfigResultId is null) {
                    ValidateWrapperApi(fileConfig);
                    LoadConfigParams(fileConfig);
                    testConfig.Api = fileConfig.Api;
                    var fileConfigResult = CreateFileConfigResult(configPath, fileConfig, runId);
                    fileConfigResultId = fileConfigResult.Id;
                }

                ValidateConfig(testConfig);

                var testResult = CreatePendingTestResult(configPath, testConfig, fileConfigResultId, 1);

                EnsureBrowser();
                RunExecutableConfig(configPath, testConfig, testResult);
            }
            finally {
                configStack.Remove(configPath);
            }
        }

        private bool RunExecutableConfig(string configPath, TestConfig fileConfig, TestResult testResult) {
            _currentConfig = fileConfig;
            LoadConfigParams(fileConfig);

            EnsureBrowser();
            var currentStepIndex = -1;
            var stepResultsByName = testResult.StepResults.ToDictionary(
                stepResult => stepResult.Name,
                StringComparer.OrdinalIgnoreCase);

            try {
                _logger.LogInformation("START API {Api}", fileConfig.Api);

                for (var stepIndex = 0; stepIndex < fileConfig.TestSteps.Count; stepIndex++) {
                    var step = fileConfig.TestSteps[stepIndex];
                    var stepNumber = stepIndex + 1;
                    currentStepIndex = stepIndex;
                    _currentStep = step;
                    _logger.LogInformation("START STEP {StepName}: {Script}", step.Name, step.SeleniumScript);

                    var dependencyResult = GetFailedDependencyResult(step, stepResultsByName);

                    if (dependencyResult is not null) {
                        var dependencyFailureResult = $"Step {dependencyResult.Step} failed";
                        UpdateStepResult(testResult, stepNumber, dependencyFailureResult, dependencyResult.Screenshot, $"Dependency step {dependencyResult.Step} ({dependencyResult.Name}) did not succeed. Result: {dependencyResult.Result}. {dependencyResult.ErrorMessage}".Trim());
                        _logger.LogInformation(
                            "SKIP STEP {StepName}: dependency {Dependency} did not succeed. Result: {Result}",
                            step.Name,
                            step.Dependency,
                            dependencyFailureResult);
                        return false;
                    }

                    var passed = RunScript(step.SeleniumScript);

                    if (!passed) {
                        var screenshot = TakeScreenshot(step.Name, "FAILED");
                        UpdateStepResult(testResult, stepNumber, "Error", screenshot, $"Selenium script returned false for step {stepNumber} ({step.Name}).");
                        _logger.LogError("FAIL STEP {StepName}. Screenshot: {Screenshot}", step.Name, screenshot);
                        return false;
                    }

                    var successScreenshot = TakeScreenshot(step.Name, "SUCCESS");
                    UpdateStepResult(testResult, stepNumber, "Success", successScreenshot, null);
                    _logger.LogInformation("PASS STEP {StepName}", step.Name);
                }

                _logger.LogInformation("END API {Api}", fileConfig.Api);
                return true;
            }
            catch (Exception ex) {
                var stepName = _currentStep?.Name ?? "Run";
                var screenshot = TakeScreenshot(stepName, "FAILED");
                EnsureFailureStepRecorded(testResult, currentStepIndex + 1, screenshot, ex.Message);
                _logger.LogError(ex, "Selenium run failed. Screenshot: {Screenshot}", screenshot);
                return false;
            }
            finally {
                _currentStep = null;
                _currentConfig = null;
            }
        }

        public void CloseBrowserSession() {
            CloseBrowser();
        }

        public void ResetSession() {
            _currentStep = null;
            _currentConfig = null;
            _sharedData.Clear();
            CloseBrowser();
        }

        public bool RunScript(string seleniumScriptPath) {
            var ownsDriver = _driver is null;

            if (ownsDriver) {
                _driver = CreateDriver();
            }

            try {
                var scriptPath = ResolveScriptPath(seleniumScriptPath);

                if (!File.Exists(scriptPath)) {
                    throw new FileNotFoundException("The Selenium script was not found.", scriptPath);
                }

                var code = File.ReadAllText(scriptPath);
                var executableCode = BuildExecutableScript(code);
                var globals = new SeleniumScriptGlobals(
                    _driver!,
                    _currentConfig,
                    _currentStep,
                    _logger,
                    _sharedData);

                var result = CSharpScript
                    .EvaluateAsync<object?>(executableCode, BuildScriptOptions(scriptPath), globals)
                    .GetAwaiter()
                    .GetResult();

                return HandleScriptResult(result, scriptPath);
            }
            finally {
                if (ownsDriver) {
                    CloseBrowser();
                }
            }
        }

        private FileConfig LoadConfig(string configPath) {
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<FileConfig>(json, options)
                ?? throw new InvalidOperationException($"File config '{configPath}' is empty or invalid.");
        }

        private TestConfig LoadTestConfig(string configPath) {
            var json = File.ReadAllText(configPath);
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<TestConfig>(json, options)
                ?? throw new InvalidOperationException($"Test config '{configPath}' is empty or invalid.");
        }

        private string ResolveTestConfigPath(string testConfigPath, string parentConfigPath) {
            if (string.IsNullOrWhiteSpace(testConfigPath)) {
                throw new InvalidOperationException($"File config '{parentConfigPath}' has an empty test path.");
            }

            if (Path.IsPathRooted(testConfigPath)) {
                var rootedPath = Path.GetFullPath(testConfigPath);

                if (!File.Exists(rootedPath)) {
                    throw new FileNotFoundException("The child test config was not found.", rootedPath);
                }

                return rootedPath;
            }

            var parentFolder = Path.GetDirectoryName(parentConfigPath) ?? ResolvePath(_folderPaths.FileConfigFolder);
            var fileName = Path.HasExtension(testConfigPath) ? testConfigPath : $"{testConfigPath}.json";
            var relativeToParent = Path.GetFullPath(Path.Combine(parentFolder, fileName));

            if (File.Exists(relativeToParent)) {
                return relativeToParent;
            }

            var configFolder = ResolvePath(_folderPaths.FileConfigFolder);
            var relativeToConfigFolder = Path.GetFullPath(Path.Combine(configFolder, fileName));

            if (File.Exists(relativeToConfigFolder)) {
                return relativeToConfigFolder;
            }

            throw new FileNotFoundException("The child test config was not found.", relativeToParent);
        }

        private void LoadConfigParams(FileConfig fileConfig) {
            foreach (var configParam in fileConfig.Params) {
                if (string.IsNullOrWhiteSpace(configParam.Key)) {
                    continue;
                }

                _sharedData[configParam.Key] = configParam.Value;
                _logger.LogInformation("Loaded config param {ConfigParamKey}", configParam.Key);
            }
        }

        private void LoadConfigParams(TestConfig testConfig) {
            foreach (var configParam in testConfig.Params) {
                if (string.IsNullOrWhiteSpace(configParam.Key)) {
                    continue;
                }

                _sharedData[configParam.Key] = configParam.Value;
                _logger.LogInformation("Loaded test config param {ConfigParamKey}", configParam.Key);
            }
        }

        private static void ValidateWrapperConfig(FileConfig fileConfig) {
            ValidateWrapperApi(fileConfig);

            if (fileConfig.Tests.Count == 0) {
                throw new InvalidOperationException("A wrapper file config requires at least one test config entry.");
            }

            foreach (var test in fileConfig.Tests) {
                if (string.IsNullOrWhiteSpace(test.TestConfig)) {
                    throw new InvalidOperationException("A wrapper file config contains an empty testConfig path.");
                }
            }
        }

        private static void ValidateWrapperApi(FileConfig fileConfig) {
            if (string.IsNullOrWhiteSpace(fileConfig.Api)) {
                throw new InvalidOperationException("The file config requires an 'api' value.");
            }
        }

        private static void ValidateConfig(TestConfig fileConfig) {
            if (string.IsNullOrWhiteSpace(fileConfig.Api)) {
                throw new InvalidOperationException("The file config requires an 'api' value.");
            }

            if (fileConfig.TestSteps.Count == 0) {
                throw new InvalidOperationException("The file config requires at least one test step, or a 'tests' list for a wrapper config.");
            }

            var seenStepNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var step in fileConfig.TestSteps) {
                if (string.IsNullOrWhiteSpace(step.Name)) {
                    throw new InvalidOperationException("Each test step requires a name.");
                }

                if (!seenStepNames.Add(step.Name)) {
                    throw new InvalidOperationException($"Step name '{step.Name}' is duplicated.");
                }
            }

            seenStepNames.Clear();

            foreach (var step in fileConfig.TestSteps) {
                if (string.IsNullOrWhiteSpace(step.SeleniumScript)) {
                    throw new InvalidOperationException($"Step '{step.Name}' requires a Selenium script path.");
                }

                if (string.IsNullOrWhiteSpace(step.Dependency)) {
                    seenStepNames.Add(step.Name);
                    continue;
                }

                if (string.Equals(step.Name, step.Dependency, StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidOperationException($"Step '{step.Name}' cannot depend on itself.");
                }

                if (!seenStepNames.Contains(step.Dependency)) {
                    throw new InvalidOperationException($"Step '{step.Name}' depends on '{step.Dependency}', but dependencies must reference an earlier step name.");
                }

                seenStepNames.Add(step.Name);
            }
        }

        private IWebDriver CreateDriver() {
            var options = new ChromeOptions();
            options.AddUserProfilePreference("credentials_enable_service", false);
            options.AddUserProfilePreference("profile.password_manager_enabled", false);
            options.AddUserProfilePreference("profile.password_manager_leak_detection", false);
            options.AddUserProfilePreference("password_manager_leak_detection", false);
            options.AddUserProfilePreference("safebrowsing.enabled", false);
            options.AddArgument("--disable-save-password-bubble");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-features=PasswordLeakDetection,PasswordManagerOnboarding,AutofillServerCommunication");
            return new ChromeDriver(options);
        }

        private void EnsureBrowser() {
            if (_driver is not null) {
                return;
            }

            _driver = CreateDriver();
        }


        private static StepResult CreateStepResult(int stepNumber, TestStep step, string result, string? screenshot) {
            return new StepResult {
                Step = stepNumber,
                Name = step.Name,
                Description = step.Description,
                Result = result,
                Screenshot = screenshot
            };
        }

        private void EnsureFailureStepRecorded(TestResult testResult, int stepNumber, string? screenshot, string? errorMessage) {
            var stepResult = testResult.StepResults.FirstOrDefault(existingStep => existingStep.Step == stepNumber);

            if (stepResult is null ||
                !string.Equals(stepResult.Result, "Not Run", StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            stepResult.Result = "Error";
            stepResult.Screenshot = screenshot;
            stepResult.ErrorMessage = TrimErrorMessage(errorMessage);
            stepResult.Description = ApplyRuntimeParams(stepResult.Description);
            _testResultRepo.UpdateStepResult(stepResult);
        }

        private static StepResult? GetFailedDependencyResult(TestStep step, IReadOnlyDictionary<string, StepResult> stepResultsByName) {
            if (string.IsNullOrWhiteSpace(step.Dependency)) {
                return null;
            }

            if (!stepResultsByName.TryGetValue(step.Dependency, out var dependencyResult) ||
                string.Equals(dependencyResult.Result, "Success", StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            return dependencyResult;
        }

        private static string GetTestConfigName(string configPath, TestConfig testConfig) {
            return string.IsNullOrWhiteSpace(testConfig.Name)
                ? Path.GetFileNameWithoutExtension(configPath)
                : testConfig.Name;
        }

        private bool HasFailedTestDependency(
            TestConfigRun testRun,
            IReadOnlyDictionary<string, TestConfigOutcome> testOutcomes,
            out string dependencyName) {
            dependencyName = testRun.Dependency ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dependencyName)) {
                return false;
            }

            if (!testOutcomes.TryGetValue(dependencyName, out var dependencyOutcome)) {
                throw new InvalidOperationException($"Test config '{GetTestConfigName(testRun.ConfigPath, testRun.Config)}' depends on '{dependencyName}', but dependencies must reference an earlier test config name in the parent fileconfig.");
            }

            return dependencyOutcome != TestConfigOutcome.Success;
        }

        private List<TestConfigRun> CreatePendingTestRuns(
            IEnumerable<FileConfigTest> tests,
            string parentConfigPath,
            int fileConfigResultId,
            string fileConfigName,
            string api) {
            var testRuns = new List<TestConfigRun>();
            var testNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var test in tests.Select((value, index) => new { value, index })) {
                var testPath = ResolveTestConfigPath(test.value.TestConfig, parentConfigPath);
                var childTestConfig = LoadTestConfig(testPath);
                childTestConfig.Api = api;
                ValidateConfig(childTestConfig);
                var testName = GetTestConfigName(testPath, childTestConfig);

                if (!testNames.Add(testName)) {
                    throw new InvalidOperationException($"Test config name '{testName}' is duplicated in file config '{fileConfigName}'.");
                }

                var testResult = CreatePendingTestResult(testPath, childTestConfig, fileConfigResultId, test.index + 1);
                testRuns.Add(new TestConfigRun(testPath, childTestConfig, test.value.Dependency, testResult));
            }

            return testRuns;
        }

        private TestResult CreatePendingTestResult(string configPath, TestConfig testConfig, int? fileConfigResultId, int step) {
            return CreateTestResult(new TestResult {
                Step = step,
                Name = GetTestConfigName(configPath, testConfig),
                Description = testConfig.Description,
                FilePath = configPath,
                FileConfigResultId = fileConfigResultId,
                StepResults = testConfig.TestSteps
                    .Select((step, index) => CreateStepResult(index + 1, step, "Not Run", null))
                    .ToList()
            });
        }

        private void MarkTestResultSkipped(TestResult testResult, string dependencyName) {
            var reason = $"Skipped because dependency test '{dependencyName}' did not succeed.";
            _logger.LogInformation(
                "SKIP TEST CONFIG {TestConfigName}: dependency {DependencyName} did not succeed.",
                testResult.Name,
                dependencyName);

            foreach (var stepResult in testResult.StepResults) {
                stepResult.Result = "Skipped";
                stepResult.Screenshot = null;
                stepResult.ErrorMessage = reason;
                stepResult.Description = ApplyRuntimeParams(string.IsNullOrWhiteSpace(stepResult.Description)
                    ? reason
                    : $"{stepResult.Description} {reason}");
                _testResultRepo.UpdateStepResult(stepResult);
            }
        }

        private FileConfigResult CreateFileConfigResult(string configPath, FileConfig fileConfig, Guid runId) {
            try {
                return _testResultRepo.CreateFileConfigResult(new FileConfigResult {
                    RunId = runId,
                    Name = string.IsNullOrWhiteSpace(fileConfig.Name) ? Path.GetFileNameWithoutExtension(configPath) : fileConfig.Name,
                    Description = fileConfig.Description,
                    FilePath = configPath
                });
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to save file config result for {FilePath}.", configPath);
                throw;
            }
        }

        private TestResult CreateTestResult(TestResult testResult) {
            try {
                return _testResultRepo.CreateTestResult(testResult);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to save test result for {FilePath}.", testResult.FilePath);
                throw;
            }
        }

        private void UpdateStepResult(TestResult testResult, int stepNumber, string result, string? screenshot, string? errorMessage) {
            var stepResult = testResult.StepResults.First(stepResult => stepResult.Step == stepNumber);
            stepResult.Result = result;
            stepResult.Screenshot = screenshot;
            stepResult.ErrorMessage = TrimErrorMessage(errorMessage);
            stepResult.Description = ApplyRuntimeParams(stepResult.Description);
            _testResultRepo.UpdateStepResult(stepResult);
        }

        private static string? TrimErrorMessage(string? errorMessage) {
            if (string.IsNullOrWhiteSpace(errorMessage)) {
                return null;
            }

            return errorMessage.Length <= 2048
                ? errorMessage
                : errorMessage[..2048];
        }

        private string? ApplyRuntimeParams(string? text) {
            if (string.IsNullOrWhiteSpace(text)) {
                return text;
            }

            return Regex.Replace(text, @"\{\{?\s*(?<key>[a-zA-Z0-9_.-]+)\s*\}?\}", match => {
                var key = match.Groups["key"].Value;

                if (_sharedData.TryGetValue(key, out var value) && value is not null) {
                    var stringValue = value.ToString();
                    return string.IsNullOrWhiteSpace(stringValue) ? match.Value : stringValue;
                }

                return match.Value;
            });
        }

        private void CloseBrowser() {
            if (_driver is null) {
                return;
            }

            try {
                _driver.Quit();
            }
            catch (WebDriverException ex) {
                _logger.LogWarning(ex, "WebDriver failed while closing the browser.");
            }
            finally {
                _driver.Dispose();
                _driver = null;
            }
        }

        private string ResolveScriptPath(string seleniumScriptPath) {
            if (Path.IsPathRooted(seleniumScriptPath)) {
                return Path.GetFullPath(seleniumScriptPath);
            }

            var directPath = ResolvePath(seleniumScriptPath);

            if (File.Exists(directPath)) {
                return directPath;
            }

            var scriptFolder = ResolvePath(_folderPaths.ScriptFolder);
            return Path.GetFullPath(Path.Combine(scriptFolder, seleniumScriptPath));
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

        private bool HandleScriptResult(object? result, string scriptPath) {
            switch (result) {
                case null:
                    return true;
                case bool passed:
                    return passed;
                case IDictionary<string, object?> runtimeParams:
                    StoreRuntimeParams(runtimeParams);
                    return true;
                case IDictionary<string, string> runtimeParams:
                    StoreRuntimeParams((IDictionary<string, object?>)runtimeParams.ToDictionary(pair => pair.Key, pair => (object?)pair.Value));
                    return true;
                case IDictionary runtimeParams:
                    StoreRuntimeParams(runtimeParams);
                    return true;
                default:
                    throw new InvalidOperationException($"Script '{scriptPath}' returned unsupported result type '{result.GetType().Name}'.");
            }
        }

        private void StoreRuntimeParams(IDictionary<string, object?> runtimeParams) {
            foreach (var runtimeParam in runtimeParams) {
                if (string.IsNullOrWhiteSpace(runtimeParam.Key)) {
                    continue;
                }

                _sharedData[runtimeParam.Key] = runtimeParam.Value;
                _logger.LogInformation("Stored runtime param {RuntimeParamKey}", runtimeParam.Key);
            }
        }

        private void StoreRuntimeParams(IDictionary runtimeParams) {
            foreach (DictionaryEntry runtimeParam in runtimeParams) {
                var key = runtimeParam.Key?.ToString();

                if (string.IsNullOrWhiteSpace(key)) {
                    continue;
                }

                _sharedData[key] = runtimeParam.Value;
                _logger.LogInformation("Stored runtime param {RuntimeParamKey}", key);
            }
        }

        private static string BuildExecutableScript(string code) {
            var hasRunMethod = Regex.IsMatch(code, @"\b[\w<>,\s\?]+\s+Run\s*\(\s*IWebDriver\s+\w+\s*\)");

            return hasRunMethod
                ? $"{code}{Environment.NewLine}Run(Driver)"
                : code;
        }

        private static ScriptOptions BuildScriptOptions(string scriptPath) {
            return ScriptOptions.Default
                .WithFilePath(scriptPath)
                .WithReferences(
                    typeof(object).Assembly,
                    typeof(Enumerable).Assembly,
                    typeof(IWebDriver).Assembly,
                    typeof(ChromeDriver).Assembly,
                    typeof(WebDriverWait).Assembly,
                    typeof(TestConfig).Assembly)
                .WithImports(
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Threading",
                    "OpenQA.Selenium",
                    "OpenQA.Selenium.Chrome",
                    "OpenQA.Selenium.Support.UI",
                    "Selenium_Handler.Vo");
        }

        private string? TakeScreenshot(string stepName, string resultSuffix) {
            if (_driver is not ITakesScreenshot screenshotDriver) {
                return null;
            }

            var screenshotsFolder = ResolvePath(_folderPaths.ScreenshotFolder);
            Directory.CreateDirectory(screenshotsFolder);

            var safeStepName = Regex.Replace(stepName, @"[^a-zA-Z0-9_-]+", "_").Trim('_');
            var safeResultSuffix = Regex.Replace(resultSuffix, @"[^a-zA-Z0-9_-]+", "_").Trim('_');
            var fileName = $"{DateTime.Now:yyyy-MM-dd_HHmmss}_{safeStepName}_{safeResultSuffix}.png";
            var filePath = Path.Combine(screenshotsFolder, fileName);

            screenshotDriver.GetScreenshot().SaveAsFile(filePath);
            return filePath;
        }

        private enum TestConfigOutcome {
            Success,
            Failure,
            Skipped
        }

        private sealed record TestConfigRun(string ConfigPath, TestConfig Config, string? Dependency, TestResult TestResult);

        public sealed class SeleniumScriptGlobals {
            public SeleniumScriptGlobals(
                IWebDriver driver,
                TestConfig? configuration,
                TestStep? step,
                ILogger logger,
                IDictionary<string, object?> sharedData) {
                Driver = driver;
                Configuration = configuration;
                Step = step;
                Logger = logger;
                SharedData = sharedData;
            }

            public IWebDriver Driver { get; }
            public TestConfig? Configuration { get; }
            public TestStep? Step { get; }
            public ILogger Logger { get; }
            public IDictionary<string, object?> SharedData { get; }
            public IDictionary<string, object?> RuntimeParams => SharedData;
        }
    }
}





