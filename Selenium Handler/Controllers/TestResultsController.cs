using Microsoft.AspNetCore.Mvc;
using Selenium_Handler.DTO;
using Selenium_Handler.Handler.Abstraction;
using Selenium_Handler.Service.Abstraction;

namespace Selenium_Handler.Controllers {
    [ApiController]
    [Route("api/test-results")]
    public class TestResultsController : ControllerBase {
        private readonly ITestResultsService _testResultsService;
        private readonly ILoggingHandler _loggingHandler;

        public TestResultsController(ITestResultsService testResultsService, ILoggingHandler loggingHandler) {
            _testResultsService = testResultsService;
            _loggingHandler = loggingHandler;
        }

        [HttpGet]
        public IActionResult GetPastTestResults([FromQuery] DateTime? start, [FromQuery] DateTime? end) {
            if (start is null) {
                return BadRequest(new {
                    Message = "The start query parameter is required."
                });
            }

            var results = _testResultsService.GetPastFileConfigResults(
                start.Value.Date,
                GetExclusiveEndDate(start.Value, end));

            return Ok(results);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetPastTestResultDetails([FromRoute] int id) {
            var result = _testResultsService.GetPastFileConfigResultDetails(id);

            if (result is null) {
                return NotFound(new {
                    Message = $"File config result '{id}' was not found."
                });
            }

            return Ok(result);
        }

        [HttpGet("tests")]
        public IActionResult GetPastChildTestResults([FromQuery] DateTime? start, [FromQuery] DateTime? end) {
            if (start is null) {
                return BadRequest(new {
                    Message = "The start query parameter is required."
                });
            }

            var results = _testResultsService.GetPastTestResults(
                start.Value.Date,
                GetExclusiveEndDate(start.Value, end));
            return Ok(results);
        }

        [HttpGet("tests/{id:int}")]
        public IActionResult GetPastChildTestResultDetails([FromRoute] int id) {
            var result = _testResultsService.GetPastTestResultDetails(id);

            if (result is null) {
                return NotFound(new {
                    Message = $"Test result '{id}' was not found."
                });
            }

            return Ok(result);
        }

        [HttpGet("download")]
        public IActionResult DownloadResult() {
            var filePath = _loggingHandler.ReturnResultPackage();
            return PhysicalFile(filePath, "application/zip", Path.GetFileName(filePath));
        }

        [HttpPost("download")]
        public IActionResult DownloadSelectedResults([FromBody] DownloadTestResultsRequest request) {
            string filePath;

            if (request.FileConfigResultIds.Length > 0) {
                filePath = _loggingHandler.ReturnResultPackageForFileConfigResults(request.FileConfigResultIds);
            }
            else if (request.TestResultIds.Length > 0) {
                filePath = _loggingHandler.ReturnResultPackage(request.TestResultIds);
            }
            else {
                filePath = _loggingHandler.ReturnResultPackage();
            }

            return PhysicalFile(filePath, "application/zip", Path.GetFileName(filePath));
        }

        private static DateTime GetExclusiveEndDate(DateTime start, DateTime? end) {
            return (end?.Date ?? start.Date).AddDays(1);
        }
    }
}
