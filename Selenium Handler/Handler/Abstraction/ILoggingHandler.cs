namespace Selenium_Handler.Handler.Abstraction {
    public interface ILoggingHandler {
        public string TakeSnapShot(); //return snapshot file name
        public string ReturnResult(); //return report's file name
        public string ReturnResult(string requestBaseUrl); //return report's file name
        public string ReturnResult(IEnumerable<int> testResultIds); //return filtered report's file name
        public string ReturnResult(IEnumerable<int> testResultIds, string requestBaseUrl); //return filtered report's file name
        public string ReturnResultPackage(); //return packaged report zip file name
        public string ReturnResultPackage(IEnumerable<int> testResultIds); //return filtered packaged report zip file name
        public string ReturnResultPackageForFileConfigResults(IEnumerable<int> fileConfigResultIds); //return filtered packaged report zip file name by top-level fileconfig result
    }
}