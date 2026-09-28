using Selenium_Handler.DTO;

namespace Selenium_Handler.Service.Abstraction {
    public interface ISeleniumService {
        public string Run(string[] fileConfig); //send a list of fileconfigs to run
        public List<FileConfigOptionDto> ListFileConfigs();
    }
}
