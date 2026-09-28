using Selenium_Handler.Handler.Abstraction;
using Selenium_Handler.Config;
using Microsoft.Extensions.Options;

namespace Selenium_Handler.Handler.Implementation {
    public class SeleniumFileHandler : ISeleniumFileHandler {
        private readonly IWebHostEnvironment _environment;
        private readonly FolderPaths _folderPaths;

        public SeleniumFileHandler(IWebHostEnvironment environment, IOptions<FolderPaths> folderPaths) {
            _environment = environment;
            _folderPaths = folderPaths.Value;
        }

        public object GetSeleniumFile(string folderName, string fileName) {
            var folderPath = ResolvePath(folderName);
            var filePath = Path.GetFullPath(Path.Combine(folderPath, fileName));

            if (!filePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("The requested file is outside the selected folder.");
            }

            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("The Selenium file was not found.", filePath);
            }

            return new FileInfo(filePath);
        }

        public List<object> GetSeleniumFile(string folderName) {
            var folderPath = ResolvePath(folderName);

            if (!Directory.Exists(folderPath)) {
                return new List<object>();
            }

            return Directory
                .EnumerateFiles(folderPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(file => (object)new FileInfo(file))
                .ToList();
        }

        public List<string> GetSeleniumFolders(string folderName) {
            var folderPath = ResolvePath(folderName);

            if (!Directory.Exists(folderPath)) {
                return new List<string>();
            }

            return Directory
                .EnumerateDirectories(folderPath, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToList();
        }

        private string ResolvePath(string folderName) {
            var configuredFolder = ResolveConfiguredFolderName(folderName);

            if (Path.IsPathRooted(configuredFolder)) {
                return Path.GetFullPath(configuredFolder);
            }

            return Path.GetFullPath(Path.Combine(ResolveBasePath(), configuredFolder));
        }

        private string ResolveBasePath() {
            return Path.GetFullPath(Path.IsPathRooted(_folderPaths.BasePath)
                ? _folderPaths.BasePath
                : Path.Combine(_environment.ContentRootPath, _folderPaths.BasePath));
        }

        private string ResolveConfiguredFolderName(string folderName) {
            return folderName.Trim() switch {
                "" => _folderPaths.BasePath,
                nameof(FolderPaths.ScriptFolder) => _folderPaths.ScriptFolder,
                nameof(FolderPaths.FileConfigFolder) => _folderPaths.FileConfigFolder,
                nameof(FolderPaths.ScreenshotFolder) => _folderPaths.ScreenshotFolder,
                nameof(FolderPaths.LogFolder) => _folderPaths.LogFolder,
                _ => folderName
            };
        }
    }
}
