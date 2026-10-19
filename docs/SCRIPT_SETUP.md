# Setting up FileConfig, tests, and Selenium scripts

This guide describes the configuration supported by the current runner. Create a parent FileConfig, add its child TestConfig files, and write the C# scripts referenced by their steps.

## 1. Folder structure

Paths below are relative to the `Selenium Handler` application folder:

```text
Scripts/
  FileConfig/
    ExampleSuite.json                  # Parent suite
    TestConfig/
      Example_OpenPage.json            # First test
      Example_VerifyPage.json          # Second test
  SeleniumScripts/
    Example_OpenPage.cs                # Browser actions
    Example_VerifyPage.cs              # Assertions
```

The contents of these three folders are ignored by Git, except for their `.gitkeep` files. Create your configs and scripts locally. Keep documentation outside these folders so Git can track it.

The folder paths come from the `Scripts` section in `appsettings.json`:

```json
{
  "Scripts": {
    "BasePath": "./Scripts",
    "FileConfigFolder": "./FileConfig",
    "ScriptFolder": "./SeleniumScripts",
    "ScreenshotFolder": "./Screenshots",
    "LogFolder": "./Logs"
  }
}
```

`BasePath` is relative to the application's content root. The other paths are relative to `BasePath`. Absolute paths are also supported.

## 2. Create the parent FileConfig

Save this as `Scripts/FileConfig/ExampleSuite.json`:

```json
{
  "api": "https://example.com/",
  "name": "Example Suite",
  "description": "Open a page, save its title, and verify it in another test.",
  "params": {
    "expectedTitle": "Example Domain"
  },
  "tests": [
    {
      "testConfig": "TestConfig/Example_OpenPage.json"
    },
    {
      "testConfig": "TestConfig/Example_VerifyPage.json",
      "dependency": "Open Example Page"
    }
  ]
}
```

| Field | Purpose |
| --- | --- |
| `api` | Required suite URL. The runner passes it to every child test as `Configuration.Api`; the script decides how to use it. |
| `name` | Suite display name. |
| `description` | Optional description. |
| `params` | Optional shared starting values. Values must be JSON strings. |
| `tests` | Ordered list of child tests. A parent suite needs at least one entry. |
| `tests[].testConfig` | Path to a child TestConfig. Relative paths resolve beside the parent FileConfig first, then under `FileConfigFolder`. |
| `tests[].dependency` | Optional **name of an earlier child test**, taken from that child's `name` field. This is not its filename. |

Child tests run in array order. A dependency must reference an earlier test in the same parent. If that test failed or was skipped, the dependent test does not run and its steps are recorded as `Skipped`. Other child tests without that dependency may still run.

Use unique child test names. If a child has no name, the runner uses its filename without the extension.

## 3. Create the child TestConfig files

Save this as `Scripts/FileConfig/TestConfig/Example_OpenPage.json`:

```json
{
  "name": "Open Example Page",
  "description": "Navigate to the suite URL and save the page title.",
  "testSteps": [
    {
      "name": "Open page and capture title",
      "description": "Open the configured URL and store pageTitle.",
      "seleniumScript": "Example_OpenPage.cs"
    }
  ],
  "chromeDriver": {
    "arguments": ["--start-maximized"],
    "userProfilePreferences": {
      "credentials_enable_service": false,
      "profile.password_manager_enabled": false
    }
  },
  "scriptOptions": {
    "references": ["object", "Enumerable", "IWebDriver", "WebDriverWait", "TestConfig"],
    "imports": [
      "System",
      "System.Collections.Generic",
      "System.Linq",
      "OpenQA.Selenium",
      "OpenQA.Selenium.Support.UI",
      "Selenium_Handler.Vo"
    ]
  }
}
```

Save this as `Scripts/FileConfig/TestConfig/Example_VerifyPage.json`:

```json
{
  "name": "Verify Example Page",
  "description": "Read the title saved by the previous test.",
  "testSteps": [
    {
      "name": "Verify saved title",
      "description": "Verify saved title {pageTitle} against {expectedTitle}.",
      "seleniumScript": "Example_VerifyPage.cs"
    }
  ],
  "scriptOptions": {
    "references": ["object", "Enumerable", "IWebDriver", "WebDriverWait", "TestConfig"],
    "imports": [
      "System",
      "System.Collections.Generic",
      "System.Linq",
      "OpenQA.Selenium",
      "OpenQA.Selenium.Support.UI",
      "Selenium_Handler.Vo"
    ]
  }
}
```

Each child requires a nonempty `testSteps` array. The parent supplies its `api`, overriding any `api` declared in the child. You may add a child `params` object to provide or override shared values when that child starts.

Each step requires a unique `name` and a `seleniumScript` path. Script filenames resolve under `ScriptFolder`; the runner also accepts absolute paths and paths relative to `BasePath` when they exist. Include the `.cs` extension.

### Dependencies between steps

To add another step after an existing one:

```json
{
  "name": "Verify heading",
  "dependency": "Open page and capture title",
  "seleniumScript": "Example_VerifyHeading.cs"
}
```

Create the referenced script before running. A step dependency names an earlier step **within the same child test**. It cannot name itself or a later step. Names and dependency matching are case-insensitive.

Steps run in array order. A script returning `false` or throwing an exception stops that child test immediately; remaining steps retain `Not Run`. A step dependency therefore does not make later steps continue after a failure. Parent-level test dependencies control whether later child tests run.

## 4. Declare script dependencies in each test JSON

Roslyn compiles and executes script text at runtime. Each TestConfig declares the assemblies and namespaces its scripts need through `scriptOptions`.

| Setting | Meaning |
| --- | --- |
| `references` | Assemblies the script can compile against. |
| `imports` | Namespaces available without writing `using` in each script. |

Supported reference aliases include `object`, `Enumerable`, `IWebDriver`, `ChromeDriver`, `WebDriverWait`, and `TestConfig`, as well as their supported fully qualified type names. Other references must match an assembly name already loaded by the application.

These lists do not install NuGet packages. Add required packages to the host application separately, then make the relevant assemblies available to the script. Dependencies are declared per child test; the parent FileConfig has no `scriptOptions` setting.

## 5. Write the Selenium scripts

Save this as `Scripts/SeleniumScripts/Example_OpenPage.cs`:

```csharp
Driver.Navigate().GoToUrl(Configuration!.Api);

var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
wait.Until(_ =>
    ((IJavaScriptExecutor)Driver).ExecuteScript("return document.readyState;")
        ?.ToString() == "complete" &&
    !string.IsNullOrWhiteSpace(Driver.Title));

return new Dictionary<string, object?>
{
    ["pageTitle"] = Driver.Title
};
```

Save this as `Scripts/SeleniumScripts/Example_VerifyPage.cs`:

```csharp
if (!RuntimeParams.TryGetValue("pageTitle", out var title))
    throw new InvalidOperationException("The previous test did not store pageTitle.");

if (!RuntimeParams.TryGetValue("expectedTitle", out var expected))
    throw new InvalidOperationException("Configure expectedTitle in the parent params.");

if (!string.Equals(title?.ToString(), expected?.ToString(), StringComparison.Ordinal))
    throw new InvalidOperationException($"Expected '{expected}', but received '{title}'.");

return true;
```

Scripts access the actual Selenium `IWebDriver`, so they can use its full API, including explicit waits, element interactions, JavaScript execution, windows, frames, alerts, and screenshots.

### Objects available to scripts

| Object | Available data |
| --- | --- |
| `Driver` | Shared Selenium `IWebDriver`. |
| `Configuration` | Current TestConfig, including `Api`, `Params`, and driver/script settings. |
| `Step` | Current step's name, description, dependency, and script path. |
| `Logger` | Application logger. Logging extension methods may require their namespace and assembly references. |
| `RuntimeParams` | Mutable shared dictionary of parent params, child params, and script-produced values. |
| `SharedData` | Alias for the same dictionary as `RuntimeParams`. |

`Configuration.Params` contains only that child's declared params. Use `RuntimeParams` to read values inherited from the parent or stored by previous scripts.

### Supported return values

| Return value | Runner behavior |
| --- | --- |
| `true` | Step passes. |
| `false` | Step fails and the child test stops. |
| `null` or no explicit return | Step passes. Prefer an explicit return for clarity. |
| A dictionary | Entries merge into `RuntimeParams` and the step passes. |
| An exception | Step fails; the runner records the error and attempts a failure screenshot. |
| Any other result type | Step fails with an unsupported-result error. |

Supported dictionaries include `Dictionary<string, object?>`, `Dictionary<string, string>`, and implementations of the non-generic `IDictionary` interface. For example, a case creation script can return:

```csharp
return new Dictionary<string, object?> { ["caseSn"] = caseSn };
```

Here, `caseSn` must already contain the value obtained by your script, such as from an API response. Later scripts or child tests can read `RuntimeParams["caseSn"]`. Direct assignments such as `RuntimeParams["caseSn"] = caseSn;` also work.

### Alternative script format and shared helpers

The runner also supports scripts that declare `bool Run(IWebDriver driver)`. It appends a call to `Run(Driver)` automatically. Use either top-level statements or the `Run` format for an entry script; do not manually call `Run(Driver)` as well.

For shared helpers, use a relative `#load` directive at the top of an entry script:

```csharp
#load "MyUi.csx"
```

Create `MyUi.csx` beside that script. A helper file contains shared methods or types rather than being listed as a test step itself. Dependencies used by loaded helpers must also be available through the child's script options or the helper's own imports.

## 6. Shared values, browser lifetime, and screenshots

- Parent params load first. Each child's params then load when that child begins, overwriting shared keys with the same name. Avoid putting a placeholder `caseSn` in later child params if it would overwrite the real returned value.
- Child tests in one parent FileConfig share the browser and runtime dictionary. The first child that creates the browser determines its Chrome options; later child driver settings do not recreate an existing browser.
- Before and after each parent FileConfig, the service resets the session: it clears runtime values and current test/step references, then quits and disposes the browser. A final reset also runs when the service run exits. Cleanup is attempted through `finally` blocks on failure.
- Saved history and screenshot files remain after session cleanup.
- The runner attempts a screenshot immediately after each script succeeds or fails. Scripts should wait for the expected UI state before returning. A pause alone does not guarantee that loading has completed.

The local Eloan scripts use `EloanUi.SettleForScreenshot()` before successful completion: it checks readiness, pauses for one second, then checks readiness again. This is an Eloan helper convention, not an automatic delay applied by the runner to every script.

Step descriptions can use `{variableName}` placeholders, such as `Created case {caseSn}`. The runner substitutes available runtime values when recording the step result. This does not substitute values into script source code.

## 7. Run the suite

Start the application with its database and browser prerequisites configured. In the web interface, select `ExampleSuite` and run it. The config list discovers top-level `.json` files in `FileConfigFolder`.

The same suite can be started by sending this JSON body to the application's `POST /api/selenium/run` endpoint:

```json
{
  "fileConfigs": ["ExampleSuite"]
}
```

The runner accepts the parent config name with or without `.json`, or its absolute path. Child tests and their steps execute sequentially. Review saved test results, step errors, and screenshots in the application's history interface.

## Troubleshooting

| Problem | Check |
| --- | --- |
| Suite does not appear | Put its `.json` directly inside `FileConfigFolder` and check configured paths. |
| Child config not found | Resolve its `testConfig` path relative to the parent file; use `TestConfig/YourTest.json`. |
| Script not found | Check the filename, extension, and configured `ScriptFolder`. |
| Dependency error | Reference an earlier test's `name` or earlier step's `name`, rather than a filename. |
| Compilation or namespace error | Check `scriptOptions.references`, `imports`, and any `#load` paths. |
| Missing runtime value | Verify that the producing script ran successfully and a later child's params did not overwrite it. |
| Browser settings seem unchanged | Chrome options apply when the shared browser is first created. |
| Screenshot captures loading UI | Add an explicit wait for the final UI state before the script returns. |
| New configs/scripts do not appear in Git status | Their contents are intentionally ignored; only folder placeholders are tracked. |
