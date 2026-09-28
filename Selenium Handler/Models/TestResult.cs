using System.ComponentModel.DataAnnotations;

namespace Selenium_Handler.Models {
    public class TestResult {
        [Key]
        public int Id { get; set; }
        public int Step { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public int? FileConfigResultId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdateTime { get; set; }
        public FileConfigResult? FileConfigResult { get; set; }
        public List<StepResult> StepResults { get; set; } = new();
    }
}
