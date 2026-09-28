namespace Selenium_Handler.Handler.Abstraction {
    public interface ISeleniumFileHandler {
        public object GetSeleniumFile(string folderName, string fileName);
        public List<object> GetSeleniumFile(string folderName);
        public List<string> GetSeleniumFolders (string folderName);

    }
}
