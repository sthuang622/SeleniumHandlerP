namespace Selenium_Handler.Handler.Abstraction {
    public interface ISeleniumHandler {
        bool RunScript(string seleniumScriptPath);
        void Run(string fileConfigPath, Guid runId);
        void CloseBrowserSession();
        void ResetSession();
        string GetFileConfigPath(string testName);
    }
}

