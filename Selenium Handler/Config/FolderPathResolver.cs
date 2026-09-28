using Microsoft.Extensions.Options;

namespace Selenium_Handler.Config {
    public class FolderPathResolver {
        private readonly IWebHostEnvironment _environment;
        private readonly FolderPaths _folderPaths;

        public FolderPathResolver(IWebHostEnvironment environment, IOptions<FolderPaths> folderPaths) {
            _environment = environment;
            _folderPaths = folderPaths.Value;
        }

        public string BasePath => ResolveBasePath();

        public string ScreenshotFolder => ResolvePath(_folderPaths.ScreenshotFolder);

        public string ResolvePath(string path) {
            if (Path.IsPathRooted(path)) {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(ResolveBasePath(), path));
        }

        public string? ToScreenshotUrl(string? screenshotPath) {
            if (string.IsNullOrWhiteSpace(screenshotPath)) {
                return null;
            }

            var screenshotsFolder = ScreenshotFolder;
            var fullScreenshotPath = Path.GetFullPath(screenshotPath);

            if (!fullScreenshotPath.StartsWith(screenshotsFolder, StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            var relativePath = Path.GetRelativePath(screenshotsFolder, fullScreenshotPath)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

            return $"/screenshots/{Uri.EscapeDataString(relativePath).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)}";
        }

        private string ResolveBasePath() {
            return Path.GetFullPath(Path.IsPathRooted(_folderPaths.BasePath)
                ? _folderPaths.BasePath
                : Path.Combine(_environment.ContentRootPath, _folderPaths.BasePath));
        }
    }
}
