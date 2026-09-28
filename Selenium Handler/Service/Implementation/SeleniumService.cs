using Microsoft.Extensions.Options;
using Selenium_Handler.Config;
using Selenium_Handler.DTO;
using Selenium_Handler.Handler.Abstraction;
using Selenium_Handler.Service.Abstraction;
using System.Text;

namespace Selenium_Handler.Service.Implementation {
    public class SeleniumService : ISeleniumService {
        private readonly ISeleniumHandler _seleniumHandler;
        private readonly ILogger<SeleniumService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly FolderPaths _folderPaths;

        public SeleniumService(
            ISeleniumHandler seleniumHandler,
            ILogger<SeleniumService> logger,
            IWebHostEnvironment environment,
            IOptions<FolderPaths> folderPaths) {
            _seleniumHandler = seleniumHandler;
            _logger = logger;
            _environment = environment;
            _folderPaths = folderPaths.Value;
        }

        public string Run(string[] fileConfig) {
            if (fileConfig.Length == 0) {
                return "No file configs were provided.";
            }

            var runId = Guid.NewGuid();
            var passed = new List<string>();
            var failed = new List<string>();

            try {
                foreach (var config in fileConfig) {
                    _seleniumHandler.ResetSession();

                    try {
                        _seleniumHandler.Run(config, runId);
                        passed.Add(config);
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Test config {Config} failed.", config);
                        failed.Add($"{config}: {ex.Message}");
                    }
                    finally {
                        _seleniumHandler.ResetSession();
                    }
                }
            }
            finally {
                _seleniumHandler.ResetSession();
            }

            var result = new StringBuilder();
            result.AppendLine($"Run id: {runId}");
            result.AppendLine($"Completed {fileConfig.Length} test config(s).");
            result.AppendLine($"Passed: {passed.Count}");
            result.AppendLine($"Failed: {failed.Count}");

            if (passed.Count > 0) {
                result.AppendLine($"Passed configs: {string.Join(", ", passed)}");
            }

            if (failed.Count > 0) {
                result.AppendLine($"Failed configs: {string.Join(", ", failed)}");
            }

            return result.ToString().TrimEnd();
        }

        public List<FileConfigOptionDto> ListFileConfigs() {
            var fileConfigFolder = ResolvePath(_folderPaths.FileConfigFolder);

            if (!Directory.Exists(fileConfigFolder)) {
                return new List<FileConfigOptionDto>();
            }

            return Directory
                .EnumerateFiles(fileConfigFolder, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(filePath => Path.GetFileName(filePath))
                .Select(filePath => new FileConfigOptionDto {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    Path = filePath
                })
                .ToList();
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
    }
}
