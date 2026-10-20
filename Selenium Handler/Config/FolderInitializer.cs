using Microsoft.Extensions.Options;

namespace Selenium_Handler.Config {
    public static class FolderInitializer {
        public static void EnsureFoldersExist(IServiceProvider services) {
            using var scope = services.CreateScope();
            var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var folderPaths = scope.ServiceProvider.GetRequiredService<IOptions<FolderPaths>>().Value;
            var basePath = ResolvePath(environment.ContentRootPath, folderPaths.BasePath);

            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(ResolvePath(basePath, folderPaths.FileConfigFolder));
            Directory.CreateDirectory(Path.Combine(ResolvePath(basePath, folderPaths.FileConfigFolder), "TestConfig"));
            Directory.CreateDirectory(ResolvePath(basePath, folderPaths.ScriptFolder));
            Directory.CreateDirectory(ResolvePath(basePath, folderPaths.ScreenshotFolder));
            Directory.CreateDirectory(ResolvePath(basePath, folderPaths.LogFolder));
        }

        private static string ResolvePath(string rootPath, string path) {
            if (Path.IsPathRooted(path)) {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(rootPath, path));
        }
    }
}
