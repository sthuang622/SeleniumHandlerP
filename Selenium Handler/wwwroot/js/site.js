const runTestsForm = document.getElementById("runTestsForm");
const fileConfigsInput = document.getElementById("fileConfigs");
const runStatus = document.getElementById("runStatus");
const runTestsButton = document.getElementById("runTestsButton");
const clearConfigsButton = document.getElementById("clearConfigsButton");
const listTestsButton = document.getElementById("listTestsButton");
const submitTestListButton = document.getElementById("submitTestListButton");
const refreshResultsButton = document.getElementById("refreshResultsButton");
const downloadResultsButton = document.getElementById("downloadResultsButton");
const resultsStartDate = document.getElementById("resultsStartDate");
const resultsList = document.getElementById("resultsList");
const testListModalElement = document.getElementById("testListModal");
const testListStatus = document.getElementById("testListStatus");
const testListOptions = document.getElementById("testListOptions");
const screenshotModalElement = document.getElementById("screenshotModal");
const screenshotModalImage = document.getElementById("screenshotModalImage");
let selectedTestNamesInClickOrder = [];

function setDefaultStartDate() {
  if (!resultsStartDate) {
    return;
  }

  const date = new Date();
  resultsStartDate.value = date.toISOString().slice(0, 10);
}

function showStatus(message, isError = false) {
  if (!runStatus) {
    return;
  }

  runStatus.hidden = false;
  runStatus.textContent = message;
  runStatus.classList.toggle("error", isError);
}

async function runTests(event) {
  event.preventDefault();

  const fileConfigs = fileConfigsInput.value
    .split(/\r?\n/)
    .map(value => value.trim())
    .filter(Boolean);

  if (fileConfigs.length === 0) {
    showStatus("Enter at least one file config.", true);
    return;
  }

  runTestsButton.disabled = true;
  showStatus("Running tests...");

  try {
    const response = await fetch("/api/selenium/run", {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ fileConfigs })
    });

    const payload = await response.json();

    if (!response.ok) {
      showStatus(payload.message ?? "Failed to run tests.", true);
      return;
    }

    showStatus(payload.message ?? "Tests completed.");
    await loadResults();
  } catch (error) {
    showStatus(error.message, true);
  } finally {
    runTestsButton.disabled = false;
  }
}

async function loadResults() {
  if (!resultsList || !resultsStartDate?.value) {
    return;
  }

  resultsList.innerHTML = '<div class="empty-state">Loading results...</div>';

  try {
    const response = await fetch(`/api/test-results?start=${encodeURIComponent(resultsStartDate.value)}`);

    if (!response.ok) {
      renderResults([]);
      return;
    }

    const payload = await tryReadJson(response);
    renderResults(payload);
  } catch (error) {
    renderResults([]);
  }
}

function renderResults(results) {
  if (!Array.isArray(results) || results.length === 0) {
    resultsList.innerHTML = '<div class="empty-state">No results found for this date range.</div>';
    return;
  }

  resultsList.innerHTML = results.map((runResult, index) => renderRunResultGroup(runResult, index === 0)).join("");

  document.querySelectorAll("[data-run-result-checkbox]").forEach(checkbox => {
    checkbox.addEventListener("click", event => event.stopPropagation());
    checkbox.addEventListener("change", () => toggleRunResultSelection(checkbox));
    updateRunResultCheckboxState(checkbox);
  });

  document.querySelectorAll(".test-result-checkbox").forEach(checkbox => {
    checkbox.addEventListener("change", () => updateParentRunResultCheckboxState(checkbox));
  });

  document.querySelectorAll("[data-test-result-details]").forEach(button => {
    button.addEventListener("click", () => loadResultDetails(button.dataset.testResultDetails));
  });
}

function renderRunResultGroup(runResult, isMostRecent) {
  const fileConfigResults = Array.isArray(runResult.fileConfigResults) ? runResult.fileConfigResults : [];
  const runElementId = `run-result-${escapeAttribute(String(runResult.runId ?? "unknown"))}`;
  const fileConfigCards = fileConfigResults.length === 0
    ? '<div class="empty-state">No fileconfig results saved for this run.</div>'
    : fileConfigResults.map(result => renderFileConfigResult(result)).join("");
  const openAttribute = isMostRecent ? " open" : "";

  return `
    <details class="run-result-group"${openAttribute}>
      <summary class="run-result-title">
        <label class="result-check" onclick="event.stopPropagation()">
          <input class="form-check-input" type="checkbox" data-run-result-checkbox="${escapeAttribute(runElementId)}" />
          <span class="run-label">Run group</span>
        </label>
        <span>${formatDate(runResult.doneDateTime)}</span>
      </summary>
      <div class="run-result-body">
        <div class="run-summary">
          <span><strong>Status:</strong> <span class="badge-result ${getResultClass(runResult.runResult)}">${escapeHtml(runResult.runResult ?? "Unknown")}</span></span>
          <span><strong>FileConfigs:</strong> ${escapeHtml(runResult.fileConfigCount ?? fileConfigResults.length)}</span>
          <span><strong>Tests:</strong> ${escapeHtml(runResult.testCount ?? 0)}</span>
          <span><strong>Test Pass/Fail:</strong> ${escapeHtml(runResult.testSuccessCount ?? 0)} / ${escapeHtml(runResult.testFailCount ?? 0)}</span>
          <span><strong>Step Pass/Fail:</strong> ${escapeHtml(runResult.stepSuccessCount ?? 0)} / ${escapeHtml(runResult.stepFailCount ?? 0)}</span>
        </div>
        <div class="run-fileconfig-list" id="${escapeAttribute(runElementId)}">
          ${fileConfigCards}
        </div>
      </div>
    </details>
  `;
}

function renderFileConfigResult(result) {
  const fileConfigResultId = result.fileConfigResultId;

  return `
    <article class="result-item">
      <div class="result-title">
        <label class="result-check">
          <input class="form-check-input test-result-checkbox" type="checkbox" value="${escapeAttribute(String(fileConfigResultId))}" data-parent-run-result="${escapeAttribute(String(result.runId ?? "unknown"))}" />
          <span>${escapeHtml(result.fileConfigName || `FileConfig #${fileConfigResultId}`)}</span>
        </label>
        <span>${formatDate(result.doneDateTime)}</span>
      </div>
      <div class="result-actions">
        <button class="btn btn-sm btn-outline-secondary" type="button" data-test-result-details="${escapeAttribute(String(fileConfigResultId))}">Details</button>
      </div>
      <div class="result-detail" id="test-result-detail-${escapeAttribute(String(fileConfigResultId))}" hidden></div>
      <table class="step-table">
        <thead>
          <tr>
            <th>Test Configs</th>
            <th>Test Success</th>
            <th>Test Fail</th>
            <th>Step Success</th>
            <th>Step Fail</th>
            <th>Status</th>
            <th>Done</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>${escapeHtml(result.testCount ?? 0)}</td>
            <td>${escapeHtml(result.testSuccessCount ?? 0)}</td>
            <td>${escapeHtml(result.testFailCount ?? 0)}</td>
            <td>${escapeHtml(result.stepSuccessCount ?? 0)}</td>
            <td>${escapeHtml(result.stepFailCount ?? 0)}</td>
            <td><span class="badge-result ${getResultClass(result.fileConfigResult)}">${escapeHtml(result.fileConfigResult ?? "Unknown")}</span></td>
            <td>${formatDate(result.doneDateTime)}</td>
          </tr>
        </tbody>
      </table>
    </article>
  `;
}

function toggleRunResultSelection(checkbox) {
  const group = document.getElementById(checkbox.dataset.runResultCheckbox);

  if (!group) {
    return;
  }

  checkbox.indeterminate = false;

  group.querySelectorAll(".test-result-checkbox").forEach(fileConfigCheckbox => {
    fileConfigCheckbox.checked = checkbox.checked;
  });
}

function updateParentRunResultCheckboxState(fileConfigCheckbox) {
  const runId = fileConfigCheckbox.dataset.parentRunResult;

  if (!runId) {
    return;
  }

  const parentCheckbox = document.querySelector(`[data-run-result-checkbox="run-result-${escapeAttribute(runId)}"]`);

  if (parentCheckbox) {
    updateRunResultCheckboxState(parentCheckbox);
  }
}

function updateRunResultCheckboxState(parentCheckbox) {
  const group = document.getElementById(parentCheckbox.dataset.runResultCheckbox);

  if (!group) {
    parentCheckbox.checked = false;
    parentCheckbox.indeterminate = false;
    return;
  }

  const childCheckboxes = Array.from(group.querySelectorAll(".test-result-checkbox"));
  const checkedCount = childCheckboxes.filter(checkbox => checkbox.checked).length;

  parentCheckbox.checked = childCheckboxes.length > 0 && checkedCount === childCheckboxes.length;
  parentCheckbox.indeterminate = checkedCount > 0 && checkedCount < childCheckboxes.length;
}

async function loadResultDetails(testId) {
  const detailContainer = document.getElementById(`test-result-detail-${testId}`);

  if (!detailContainer) {
    return;
  }

  if (!detailContainer.hidden) {
    detailContainer.hidden = true;
    return;
  }

  detailContainer.hidden = false;
  detailContainer.innerHTML = '<div class="empty-state">Loading details...</div>';

  try {
    const response = await fetch(`/api/test-results/${encodeURIComponent(testId)}`);

    if (!response.ok) {
      detailContainer.innerHTML = renderResultDetails({ testId, stepResults: [] });
      bindDetailActions(detailContainer);
      return;
    }

    const payload = await tryReadJson(response);
    detailContainer.innerHTML = renderResultDetails(payload);
    bindDetailActions(detailContainer);
  } catch (error) {
    detailContainer.innerHTML = renderResultDetails({ testId, stepResults: [] });
    bindDetailActions(detailContainer);
  }
}

async function downloadCheckedResults() {
  const selectedIds = Array
    .from(document.querySelectorAll(".test-result-checkbox:checked"))
    .map(checkbox => Number.parseInt(checkbox.value, 10))
    .filter(Number.isInteger);

  if (selectedIds.length === 0) {
    showStatus("Check at least one test result before downloading.", true);
    return;
  }

  downloadResultsButton.disabled = true;
  showStatus("Preparing selected result report...");

  try {
    const response = await fetch("/api/test-results/download", {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ fileConfigResultIds: selectedIds })
    });

    if (!response.ok) {
      const payload = await tryReadJson(response);
      showStatus(payload?.message ?? "Failed to download selected results.", true);
      return;
    }

    const blob = await response.blob();
    const downloadUrl = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = downloadUrl;
    link.download = getDownloadFileName(response.headers.get("content-disposition"));
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(downloadUrl);
    showStatus(`Downloaded ${selectedIds.length} selected top-level result(s).`);
  } catch (error) {
    showStatus(error.message, true);
  } finally {
    downloadResultsButton.disabled = false;
  }
}

async function openTestListModal() {
  if (!testListModalElement || !testListStatus || !testListOptions) {
    return;
  }

  selectedTestNamesInClickOrder = [];
  testListStatus.hidden = false;
  testListStatus.textContent = "Loading tests...";
  testListOptions.innerHTML = "";
  getTestListModal().show();

  try {
    const response = await fetch("/api/selenium/file-configs");

    if (!response.ok) {
      renderTestList([]);
      return;
    }

    const payload = await tryReadJson(response);
    renderTestList(payload);
  } catch (error) {
    renderTestList([]);
  }
}

function renderTestList(fileConfigs) {
  if (!testListStatus || !testListOptions) {
    return;
  }

  if (!Array.isArray(fileConfigs) || fileConfigs.length === 0) {
    testListStatus.hidden = false;
    testListStatus.textContent = "No file configs were found.";
    testListOptions.innerHTML = "";
    selectedTestNamesInClickOrder = [];
    return;
  }

  testListStatus.hidden = true;
  selectedTestNamesInClickOrder = [];
  testListOptions.innerHTML = fileConfigs.map((fileConfig, index) => {
    const id = `file-config-option-${index}`;
    const name = fileConfig.name ?? "";

    return `
      <label class="test-list-option" for="${escapeAttribute(id)}">
        <input class="form-check-input file-config-checkbox" id="${escapeAttribute(id)}" type="checkbox" value="${escapeHtml(name)}" />
        <span>
          <strong>${escapeHtml(name)}</strong>
          <small>${escapeHtml(fileConfig.path ?? "")}</small>
        </span>
      </label>
    `;
  }).join("");

  document.querySelectorAll(".file-config-checkbox").forEach(checkbox => {
    checkbox.addEventListener("change", () => updateSelectedTestOrder(checkbox));
  });
}

function updateSelectedTestOrder(checkbox) {
  const name = checkbox.value;

  if (!name) {
    return;
  }

  selectedTestNamesInClickOrder = selectedTestNamesInClickOrder.filter(selectedName => selectedName !== name);

  if (checkbox.checked) {
    selectedTestNamesInClickOrder.push(name);
  }
}

function submitSelectedTests() {
  const selectedNames = selectedTestNamesInClickOrder.filter(name => {
    const checkbox = Array
      .from(document.querySelectorAll(".file-config-checkbox"))
      .find(option => option.value === name);

    return checkbox?.checked;
  });

  if (selectedNames.length === 0) {
    if (testListStatus) {
      testListStatus.hidden = false;
      testListStatus.textContent = "Select at least one test.";
    }
    return;
  }

  fileConfigsInput.value = selectedNames.join("\n");
  getTestListModal().hide();
  showStatus(`Set ${selectedNames.length} selected test(s) in click order.`);
}

function renderResultDetails(result) {
  const tests = Array.isArray(result.testConfigs) ? result.testConfigs : [];

  if (tests.length > 0) {
    const testSections = tests.map(test => renderChildTestDetails(test)).join("");

    return `
      <div class="detail-meta">
        <div><strong>${escapeHtml(result.name ?? `FileConfig #${result.fileConfigResultId}`)}</strong></div>
        <div>${escapeHtml(result.description ?? "")}</div>
        <div class="result-path">${escapeHtml(result.filePath ?? "")}</div>
      </div>
      ${testSections}
    `;
  }

  return renderChildTestDetails(result);
}

function renderChildTestDetails(result) {
  const steps = Array.isArray(result.stepResults) ? result.stepResults : [];
  const rows = steps.length === 0
    ? '<tr><td colspan="6">No step results saved.</td></tr>'
    : steps.map(step => `
        <tr>
          <td>${escapeHtml(step.step ?? "N/A")}</td>
          <td>${escapeHtml(step.name ?? "N/A")}</td>
          <td>${escapeHtml(step.description ?? "")}</td>
          <td>${renderStepResultBadge(step)}</td>
          <td>${renderScreenshotPreview(step.screenshot)}</td>
          <td>${formatDate(step.updateTime ?? step.createdAt)}</td>
        </tr>
      `).join("");

  return `
    <div class="detail-meta">
      <div><strong>${escapeHtml(result.step ? `Test ${result.step}: ` : "")}${escapeHtml(result.name ?? `Test #${result.testId}`)}</strong></div>
      <div>${escapeHtml(result.description ?? "")}</div>
      <div class="result-path">Updated: ${formatDate(result.updateTime ?? result.testDoneDateTime)}</div>
      <div class="result-path">${escapeHtml(result.filePath ?? "")}</div>
    </div>
    <table class="step-table">
      <thead>
        <tr>
          <th>Step</th>
          <th>Name</th>
          <th>Description</th>
          <th>Result</th>
          <th>Screenshot</th>
          <th>Updated</th>
        </tr>
      </thead>
      <tbody>${rows}</tbody>
    </table>
  `;
}

function formatDate(value) {
  if (!value) {
    return "N/A";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return String(value);
  }

  return date.toLocaleString();
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function escapeAttribute(value) {
  return value.replace(/[^a-z0-9_-]/gi, "");
}

function getResultClass(value) {
  const result = String(value ?? "").trim().toLowerCase();

  if (result.includes("failed")) {
    return "failed";
  }

  return escapeAttribute(result.replace(/\s+/g, "-"));
}

function renderStepResultBadge(step) {
  const result = step.result ?? "Unknown";
  const errorMessage = step.errorMessage ?? "";
  const resultClass = getResultClass(result);

  if (!errorMessage) {
    return `<span class="badge-result ${resultClass}">${escapeHtml(result)}</span>`;
  }

  return `
    <button class="badge-result badge-result-button ${resultClass}" type="button" title="${escapeAttributeValue(errorMessage)}" data-error-message="${escapeAttributeValue(errorMessage)}">
      ${escapeHtml(result)}
    </button>
  `;
}

function renderScreenshotPreview(value) {
  if (!value) {
    return "N/A";
  }

  const href = getScreenshotUrl(value);
  const label = getScreenshotLabel(value);

  return `
    <button class="screenshot-thumb-button" type="button" data-screenshot-src="${escapeAttributeValue(href)}" data-screenshot-label="${escapeAttributeValue(label)}">
      <img class="screenshot-thumb" src="${escapeAttributeValue(href)}" alt="${escapeAttributeValue(label)}" loading="lazy" />
    </button>
  `;
}

function getScreenshotUrl(value) {
  const raw = String(value ?? "").trim();

  if (!raw) {
    return "";
  }

  if (raw.startsWith("/") || /^https?:\/\//i.test(raw)) {
    return raw;
  }

  const fileName = getScreenshotLabel(raw);
  return `/screenshots/${encodeURIComponent(fileName)}`;
}

function getScreenshotLabel(value) {
  return String(value ?? "")
    .split(/[\\/]/)
    .filter(Boolean)
    .at(-1) ?? "Screenshot";
}

function escapeAttributeValue(value) {
  return escapeHtml(value).replaceAll("`", "&#096;");
}

function bindDetailActions(root) {
  bindScreenshotPreviewButtons(root);
  bindErrorMessageButtons(root);
}

function bindScreenshotPreviewButtons(root) {
  root.querySelectorAll("[data-screenshot-src]").forEach(button => {
    button.addEventListener("click", () => openScreenshotModal(button.dataset.screenshotSrc, button.dataset.screenshotLabel));
  });
}

function bindErrorMessageButtons(root) {
  root.querySelectorAll("[data-error-message]").forEach(button => {
    button.addEventListener("click", () => copyErrorMessage(button.dataset.errorMessage));
  });
}

async function copyErrorMessage(errorMessage) {
  if (!errorMessage) {
    return;
  }

  try {
    await navigator.clipboard.writeText(errorMessage);
    showStatus("Copied error message.");
  } catch (error) {
    showStatus("Could not copy error message.", true);
  }
}

function openScreenshotModal(src, label) {
  if (!screenshotModalElement || !screenshotModalImage || !src) {
    return;
  }

  const title = document.getElementById("screenshotModalLabel");

  if (title) {
    title.textContent = label || "Screenshot";
  }

  screenshotModalImage.src = src;
  screenshotModalImage.alt = label || "Screenshot preview";
  bootstrap.Modal.getOrCreateInstance(screenshotModalElement).show();
}

function getTestListModal() {
  return bootstrap.Modal.getOrCreateInstance(testListModalElement);
}

function getDownloadFileName(contentDisposition) {
  const fallback = "selenium-result.html";

  if (!contentDisposition) {
    return fallback;
  }

  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);

  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const fileNameMatch = contentDisposition.match(/filename="?([^"]+)"?/i);
  return fileNameMatch?.[1] ?? fallback;
}

async function tryReadJson(response) {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

if (runTestsForm) {
  setDefaultStartDate();
  runTestsForm.addEventListener("submit", runTests);
  clearConfigsButton?.addEventListener("click", () => {
    fileConfigsInput.value = "";
    runStatus.hidden = true;
  });
  refreshResultsButton?.addEventListener("click", loadResults);
  listTestsButton?.addEventListener("click", openTestListModal);
  submitTestListButton?.addEventListener("click", submitSelectedTests);
  downloadResultsButton?.addEventListener("click", downloadCheckedResults);
  resultsStartDate?.addEventListener("change", loadResults);
  loadResults();
}
