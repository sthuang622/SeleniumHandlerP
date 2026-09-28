using System.ComponentModel.DataAnnotations;

namespace Selenium_Handler.Models {
    public class FileConfigResult {
        [Key]
        public int Id { get; set; }
        public Guid RunId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<TestResult> TestResults { get; set; } = new();
    }
}
