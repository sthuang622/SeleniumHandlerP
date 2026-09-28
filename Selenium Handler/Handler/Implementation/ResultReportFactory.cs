using Selenium_Handler.Models;
using System.Net;
using System.Text;

namespace Selenium_Handler.Handler.Implementation {
    public static class ResultReportFactory {
        public static string BuildResultReport(IReadOnlyCollection<TestResult> testResults, Func<string?, string?> screenshotUrlFactory) {
            var report = new StringBuilder();
            var runGroups = BuildRunResultGroups(testResults);

            var totalSuccess = testResults.Sum(testResult => testResult.StepResults.Count(stepResult =>
                IsSuccessResult(stepResult.Result)));
            var totalFailed = testResults.Sum(testResult => testResult.StepResults.Count(stepResult =>
                IsFailureResult(stepResult.Result)));
            var totalNotRun = testResults.Sum(testResult => testResult.StepResults.Count(stepResult =>
                IsNotRunResult(stepResult.Result)));

            report.AppendLine("<!doctype html>");
            report.AppendLine("<html lang=\"en\">");
            report.AppendLine("<head>");
            report.AppendLine("<meta charset=\"utf-8\" />");
            report.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            report.AppendLine("<title>Selenium Handler Result Report</title>");
            report.AppendLine("<style>");
            report.AppendLine("body{margin:0;background:#f6f7f9;color:#1f2937;font-family:Segoe UI,Arial,sans-serif;}");
            report.AppendLine(".wrap{max-width:1120px;margin:0 auto;padding:32px 20px;}");
            report.AppendLine("h1{margin:0;font-size:30px;} .sub{margin-top:8px;color:#596273;}");
            report.AppendLine(".summary{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;margin:24px 0;}");
            report.AppendLine(".metric{background:#fff;border:1px solid #d9dee7;border-radius:8px;box-shadow:0 8px 24px rgba(31,41,55,.06);padding:18px;} .metric strong{display:block;font-size:28px;margin-top:4px;} .metric span{color:#697386;}");
            report.AppendLine(".run-group{margin:24px 0;border:2px solid #8fb3dc;border-radius:12px;background:#fff;box-shadow:0 8px 24px rgba(31,41,55,.08);overflow:hidden;}");
            report.AppendLine(".run-summary{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:16px;padding:18px;background:#eaf3ff;border-bottom:1px solid #bfd5ee;cursor:pointer;list-style:none;} .run-summary::-webkit-details-marker{display:none;}");
            report.AppendLine(".run-title{font-size:20px;font-weight:800;} .run-body{display:grid;gap:16px;padding:18px;background:#f8fbff;}");
            report.AppendLine(".file-config{border:1px solid #b8c7da;border-radius:10px;background:#fff;overflow:hidden;} .file-config-summary{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:16px;padding:16px;background:#f1f6fd;border-bottom:1px solid #d8e3f1;cursor:pointer;list-style:none;} .file-config-summary::-webkit-details-marker{display:none;} .file-config-body{display:grid;gap:12px;padding:14px 16px 16px;}");
            report.AppendLine(".child-test{border:1px solid #d6e0ed;border-radius:8px;background:#fbfdff;overflow:hidden;} .child-test-summary{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:16px;padding:14px 16px;background:#f8fafc;border-bottom:1px solid #e5e9f0;cursor:pointer;list-style:none;} .child-test-summary::-webkit-details-marker{display:none;} .child-test-body{padding:12px 16px 16px;background:#fff;}");
            report.AppendLine(".file-config-title{font-size:18px;font-weight:700;} .child-test h2{font-size:16px;margin:0;} .id-pill{display:inline-block;margin-left:6px;padding:2px 8px;border-radius:999px;background:#e2e8f0;color:#475569;font-size:12px;font-weight:700;vertical-align:middle;} .muted{color:#697386;} .path{overflow-wrap:anywhere;color:#4f5b6b;font-size:13px;margin-top:6px;}");
            report.AppendLine("table{width:100%;border-collapse:collapse;margin-top:14px;font-size:14px;} th,td{padding:8px 10px;border-top:1px solid #e5e9f0;text-align:left;vertical-align:top;}");
            report.AppendLine(".badge{display:inline-block;min-width:72px;padding:3px 8px;border-radius:999px;background:#edf2f7;text-align:center;font-weight:700;} .badge.has-error{cursor:help;}");
            report.AppendLine(".success,.passed{background:#dff5e7;color:#17633a;} .failed,.error{background:#fde2e2;color:#8a1f1f;} .not-run{background:#edf2f7;color:#4f5b6b;}");
            report.AppendLine(".screenshot-thumb-button{display:inline-flex;padding:0;border:1px solid #cbd5e1;border-radius:8px;background:#fff;cursor:zoom-in;box-shadow:0 2px 8px rgba(31,41,55,.08);} .screenshot-thumb-button:hover{border-color:#64748b;} .screenshot-thumb{width:112px;height:72px;object-fit:cover;border-radius:7px;display:block;}");
            report.AppendLine(".screenshot-modal{position:fixed;inset:0;display:none;align-items:center;justify-content:center;background:rgba(15,23,42,.82);z-index:9999;padding:24px;} .screenshot-modal.open{display:flex;} .screenshot-modal-content{max-width:min(1200px,96vw);max-height:90vh;} .screenshot-modal-title{color:#fff;font-weight:700;margin:0 48px 10px 0;overflow-wrap:anywhere;} .screenshot-modal-content img{max-width:100%;max-height:82vh;object-fit:contain;background:#fff;border-radius:8px;box-shadow:0 18px 48px rgba(0,0,0,.35);} .screenshot-modal-close{position:absolute;top:16px;right:16px;border:0;border-radius:999px;padding:8px 12px;background:#fff;color:#111827;font-weight:700;cursor:pointer;}");
            report.AppendLine("@media(max-width:720px){.summary{grid-template-columns:1fr}.run-summary,.file-config-summary,.child-test-summary{grid-template-columns:1fr}.screenshot-thumb{width:88px;height:58px}}");
            report.AppendLine("</style>");
            report.AppendLine("</head>");
            report.AppendLine("<body><main class=\"wrap\">");
            report.AppendLine("<h1>Selenium Handler Result Report</h1>");
            report.AppendLine($"<div class=\"sub\">Generated at {Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
            report.AppendLine("<section class=\"summary\">");
            report.AppendLine($"<div class=\"metric\"><span>Runs</span><strong>{runGroups.Count}</strong></div>");
            report.AppendLine($"<div class=\"metric\"><span>Success steps</span><strong>{totalSuccess}</strong></div>");
            report.AppendLine($"<div class=\"metric\"><span>Failed steps</span><strong>{totalFailed}</strong></div>");
            report.AppendLine($"<div class=\"metric\"><span>Not run steps</span><strong>{totalNotRun}</strong></div>");
            report.AppendLine("</section>");

            if (testResults.Count == 0) {
                report.AppendLine("<section class=\"run-group muted\">No test results were found.</section>");
                AppendScreenshotModal(report);
                report.AppendLine("</main></body></html>");
                return report.ToString();
            }

            foreach (var runGroup in runGroups) {
                report.AppendLine("<details class=\"run-group\" open>");
                report.AppendLine("<summary class=\"run-summary\">");
                report.AppendLine("<div>");
                report.AppendLine($"<div class=\"run-title\">Run <span class=\"id-pill\">{runGroup.FileConfigResults.Count} fileconfig row(s)</span></div>");
                report.AppendLine($"<div class=\"muted\">Status: {Html(GetRunResultLabel(runGroup.FileConfigResults))}</div>");
                report.AppendLine("</div>");
                report.AppendLine($"<div class=\"muted\">{Html(runGroup.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
                report.AppendLine("</summary>");
                report.AppendLine("<div class=\"run-body\">");

                foreach (var fileConfigGroup in runGroup.FileConfigResults) {
                    AppendFileConfigGroup(report, fileConfigGroup, screenshotUrlFactory);
                }

                report.AppendLine("</div>");
                report.AppendLine("</details>");
            }

            AppendScreenshotModal(report);
            report.AppendLine("</main></body></html>");
            return report.ToString();
        }

        private static void AppendFileConfigGroup(StringBuilder report, FileConfigResultGroup fileConfigGroup, Func<string?, string?> screenshotUrlFactory) {
            report.AppendLine("<details class=\"file-config\" open>");
            report.AppendLine("<summary class=\"file-config-summary\">");
            report.AppendLine("<div>");
            report.AppendLine($"<div class=\"file-config-title\">{Html(fileConfigGroup.Name)} <span class=\"id-pill\">FileConfig row #{fileConfigGroup.Id}</span></div>");
            report.AppendLine($"<div class=\"muted\">{Html(fileConfigGroup.Description ?? "No description")}</div>");
            report.AppendLine($"<div class=\"muted\">Status: {Html(GetFileConfigResultLabel(fileConfigGroup.TestResults))}</div>");
            report.AppendLine("</div>");
            report.AppendLine($"<div class=\"muted\">{Html(fileConfigGroup.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
            report.AppendLine("</summary>");
            report.AppendLine("<div class=\"file-config-body\">");
            report.AppendLine($"<div class=\"path\">{Html(fileConfigGroup.FilePath)}</div>");

            foreach (var testResult in fileConfigGroup.TestResults.OrderBy(testResult => testResult.Step).ThenBy(testResult => testResult.Id)) {
                AppendTestResult(report, testResult, screenshotUrlFactory);
            }

            report.AppendLine("</div>");
            report.AppendLine("</details>");
        }

        private static void AppendTestResult(StringBuilder report, TestResult testResult, Func<string?, string?> screenshotUrlFactory) {
            var childTestTitle = testResult.Step > 0
                ? $"Test {testResult.Step}: {testResult.Name}"
                : testResult.Name;

            report.AppendLine("<details class=\"child-test\" open>");
            report.AppendLine("<summary class=\"child-test-summary\">");
            report.AppendLine("<div>");
            report.AppendLine($"<h2>{Html(childTestTitle)} <span class=\"id-pill\">Test row #{testResult.Id}</span></h2>");
            report.AppendLine($"<div class=\"muted\">{Html(testResult.Description ?? "No description")}</div>");
            report.AppendLine($"<div class=\"muted\">Status: {Html(GetTestResultLabel(testResult.StepResults))}</div>");
            report.AppendLine("</div>");
            report.AppendLine($"<div class=\"muted\">{Html(testResult.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
            report.AppendLine("</summary>");
            report.AppendLine("<div class=\"child-test-body\">");
            report.AppendLine($"<div class=\"path\">{Html(testResult.FilePath)}</div>");
            report.AppendLine("<table><thead><tr><th>Step</th><th>Name</th><th>Description</th><th>Result</th><th>Screenshot</th><th>Updated</th></tr></thead><tbody>");

            foreach (var stepResult in testResult.StepResults.OrderBy(stepResult => stepResult.Step).ThenBy(stepResult => stepResult.Id)) {
                var resultClass = Html(GetResultClass(stepResult.Result));
                var errorTitle = Html(stepResult.ErrorMessage ?? string.Empty);
                var errorClass = string.IsNullOrWhiteSpace(stepResult.ErrorMessage) ? string.Empty : " has-error";
                var errorLabel = string.IsNullOrWhiteSpace(stepResult.ErrorMessage) ? string.Empty : $" aria-label=\"{errorTitle}\"";
                report.AppendLine("<tr>");
                report.AppendLine($"<td>{stepResult.Step}</td>");
                report.AppendLine($"<td>{Html(stepResult.Name)}</td>");
                report.AppendLine($"<td>{Html(stepResult.Description ?? "N/A")}</td>");
                report.AppendLine($"<td><span class=\"badge {resultClass}{errorClass}\" title=\"{errorTitle}\"{errorLabel}>{Html(stepResult.Result)}</span></td>");
                report.AppendLine($"<td>{BuildScreenshotPreview(stepResult.Screenshot, screenshotUrlFactory)}</td>");
                report.AppendLine($"<td>{Html(stepResult.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss"))}</td>");
                report.AppendLine("</tr>");
            }

            report.AppendLine("</tbody></table>");
            report.AppendLine("</div>");
            report.AppendLine("</details>");
        }

        private static List<RunResultGroup> BuildRunResultGroups(IReadOnlyCollection<TestResult> testResults) {
            return testResults
                .GroupBy(GetRunGroupKey)
                .Select(group => {
                    var fileConfigGroups = BuildFileConfigResultGroups(group.ToList());

                    return new RunResultGroup(
                        group.Key,
                        fileConfigGroups.Max(fileConfigGroup => fileConfigGroup.CreatedAt),
                        fileConfigGroups.Max(fileConfigGroup => fileConfigGroup.UpdateTime),
                        fileConfigGroups);
                })
                .OrderBy(runGroup => runGroup.UpdateTime)
                .ToList();
        }

        private static string GetRunGroupKey(TestResult testResult) {
            if (testResult.FileConfigResult is not null) {
                return testResult.FileConfigResult.RunId.ToString("D");
            }

            return $"standalone-{testResult.Id}";
        }

        private static List<FileConfigResultGroup> BuildFileConfigResultGroups(IReadOnlyCollection<TestResult> testResults) {
            return testResults
                .GroupBy(testResult => testResult.FileConfigResultId ?? -testResult.Id)
                .Select(group => {
                    var orderedTests = group
                        .OrderBy(testResult => testResult.Step)
                        .ThenBy(testResult => testResult.Id)
                        .ToList();
                    var firstTest = orderedTests.First();
                    var fileConfigResult = firstTest.FileConfigResult;
                    var fileConfigResultId = firstTest.FileConfigResultId ?? firstTest.Id;

                    return new FileConfigResultGroup(
                        fileConfigResultId,
                        fileConfigResult?.Name ?? firstTest.Name,
                        fileConfigResult?.Description ?? firstTest.Description,
                        fileConfigResult?.FilePath ?? firstTest.FilePath,
                        fileConfigResult?.CreatedAt ?? firstTest.CreatedAt,
                        orderedTests.Max(testResult => testResult.UpdateTime),
                        orderedTests);
                })
                .OrderBy(group => group.FileConfigResultId)
                .ThenBy(group => group.CreatedAt)
                .ToList();
        }

        private sealed record RunResultGroup(
            string RunGroupKey,
            DateTime CreatedAt,
            DateTime UpdateTime,
            List<FileConfigResultGroup> FileConfigResults);

        private sealed record FileConfigResultGroup(
            int FileConfigResultId,
            string Name,
            string? Description,
            string FilePath,
            DateTime CreatedAt,
            DateTime UpdateTime,
            List<TestResult> TestResults) {
            public int Id => FileConfigResultId;
        }

        private static void AppendScreenshotModal(StringBuilder report) {
            report.AppendLine("<div class=\"screenshot-modal\" id=\"screenshotModal\" aria-hidden=\"true\">");
            report.AppendLine("<button class=\"screenshot-modal-close\" type=\"button\">Close</button>");
            report.AppendLine("<div class=\"screenshot-modal-content\">");
            report.AppendLine("<div class=\"screenshot-modal-title\" id=\"screenshotModalTitle\">Screenshot</div>");
            report.AppendLine("<img id=\"screenshotModalImage\" alt=\"Screenshot preview\" />");
            report.AppendLine("</div>");
            report.AppendLine("</div>");
            report.AppendLine("<script>");
            report.AppendLine("(() => {");
            report.AppendLine("const modal = document.getElementById('screenshotModal');");
            report.AppendLine("const image = document.getElementById('screenshotModalImage');");
            report.AppendLine("const title = document.getElementById('screenshotModalTitle');");
            report.AppendLine("if (!modal || !image || !title) return;");
            report.AppendLine("const close = () => { modal.classList.remove('open'); modal.setAttribute('aria-hidden','true'); image.removeAttribute('src'); };");
            report.AppendLine("document.querySelectorAll('[data-screenshot-src]').forEach(button => {");
            report.AppendLine("button.addEventListener('click', () => {");
            report.AppendLine("image.src = button.dataset.screenshotSrc || ''; title.textContent = button.dataset.screenshotLabel || 'Screenshot'; modal.classList.add('open'); modal.setAttribute('aria-hidden','false');");
            report.AppendLine("});");
            report.AppendLine("});");
            report.AppendLine("modal.addEventListener('click', event => { if (event.target === modal) close(); });");
            report.AppendLine("document.querySelector('.screenshot-modal-close')?.addEventListener('click', close);");
            report.AppendLine("document.addEventListener('keydown', event => { if (event.key === 'Escape') close(); });");
            report.AppendLine("})();");
            report.AppendLine("</script>");
        }

        private static string Html(string value) {
            return WebUtility.HtmlEncode(value);
        }

        private static string BuildScreenshotPreview(string? screenshotPath, Func<string?, string?> screenshotUrlFactory) {
            var screenshotUrl = screenshotUrlFactory(screenshotPath);

            if (string.IsNullOrWhiteSpace(screenshotUrl)) {
                return "N/A";
            }

            var label = Path.GetFileName(screenshotPath) ?? "Screenshot";
            var escapedUrl = Html(screenshotUrl);
            var escapedLabel = Html(label);

            return $"<button class=\"screenshot-thumb-button\" type=\"button\" data-screenshot-src=\"{escapedUrl}\" data-screenshot-label=\"{escapedLabel}\"><img class=\"screenshot-thumb\" src=\"{escapedUrl}\" alt=\"{escapedLabel}\" loading=\"lazy\" /></button>";
        }

        private static string GetRunResultLabel(IReadOnlyCollection<FileConfigResultGroup> fileConfigResults) {
            var testResults = fileConfigResults.SelectMany(fileConfigResult => fileConfigResult.TestResults).ToList();
            return GetFileConfigResultLabel(testResults);
        }

        private static string GetFileConfigResultLabel(IReadOnlyCollection<TestResult> testResults) {
            if (testResults.Count == 0) {
                return "Not Run";
            }

            var labels = testResults.Select(testResult => GetTestResultLabel(testResult.StepResults)).ToList();

            if (labels.Any(label => string.Equals(label, "Failure", StringComparison.OrdinalIgnoreCase))) {
                return "Failure";
            }

            if (labels.Any(label => string.Equals(label, "Not Run", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(label, "Skipped", StringComparison.OrdinalIgnoreCase))) {
                return "Not Run";
            }

            return "Success";
        }

        private static string GetTestResultLabel(IReadOnlyCollection<StepResult> stepResults) {
            if (stepResults.Count == 0 ||
                stepResults.All(stepResult => IsNotRunResult(stepResult.Result))) {
                return "Not Run";
            }

            if (stepResults.Any(stepResult => IsFailureResult(stepResult.Result))) {
                return "Failure";
            }

            if (stepResults.All(stepResult => IsSkippedResult(stepResult.Result))) {
                return "Skipped";
            }

            if (stepResults.Any(stepResult => IsNotRunResult(stepResult.Result) || IsSkippedResult(stepResult.Result))) {
                return "Not Run";
            }

            return "Success";
        }

        private static string GetResultClass(string result) {
            if (IsFailureResult(result)) {
                return "failed";
            }

            return result.Trim().ToLowerInvariant().Replace(" ", "-");
        }

        private static bool IsSuccessResult(string result) {
            return string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(result, "Passed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFailureResult(string result) {
            return string.Equals(result, "Failed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(result, "Error", StringComparison.OrdinalIgnoreCase) ||
                   result.Contains("failed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNotRunResult(string result) {
            return string.Equals(result, "Not Run", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSkippedResult(string result) {
            return string.Equals(result, "Skipped", StringComparison.OrdinalIgnoreCase);
        }
    }
}
