using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Selenium_Handler.Models {
    public class StepResult {
        [Key]
        public int Id { get; set; }
        public int TestResultId { get; set; }
        public int Step { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Screenshot { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdateTime { get; set; }
        [JsonIgnore]
        public TestResult? TestResult { get; set; }
    }
}
