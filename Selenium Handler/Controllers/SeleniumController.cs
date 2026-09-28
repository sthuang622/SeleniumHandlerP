using Microsoft.AspNetCore.Mvc;
using Selenium_Handler.DTO;
using Selenium_Handler.Service.Abstraction;

namespace Selenium_Handler.Controllers {
    [ApiController]
    [Route("api/selenium")]
    public class SeleniumController : ControllerBase {
        private readonly ISeleniumService _seleniumService;

        public SeleniumController(ISeleniumService seleniumService) {
            _seleniumService = seleniumService;
        }

        [HttpPost("run")]
        public IActionResult Run([FromBody] RunSeleniumRequest request) {
            if (request.FileConfigs.Length == 0) {
                return BadRequest(new {
                    Message = "At least one file config is required."
                });
            }

            var result = _seleniumService.Run(request.FileConfigs);

            return Ok(new {
                Message = result
            });
        }

        [HttpGet("file-configs")]
        public IActionResult ListFileConfigs() {
            return Ok(_seleniumService.ListFileConfigs());
        }
    }
}
