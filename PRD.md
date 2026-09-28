# Product Requirements Document

## 1. Product Overview

### Product Name

Selenium Handler

### Purpose

Selenium Handler is a developer-focused tool for running Selenium automation from configuration files. The JSON file config defines basic test information and the ordered list of test steps. Each step points to a Selenium C# file that contains the actual browser automation commands.

The application should not describe every Selenium operation in JSON. Its responsibility is to read the file config, load each listed Selenium file, call it in order, and record the result.

## 2. Core Idea

The project separates test flow from Selenium implementation.

The file config answers:

- What API or target flow is being tested?
- What steps belong to the test?
- What order should those steps run in?
- Which Selenium file should be called for each step?

The Selenium files answer:

- Which browser elements should be found?
- Which Selenium commands should run?
- Which waits, conditions, loops, and checks are needed?
- Whether the step passed or failed?

This keeps the JSON simple and lets Selenium code stay in normal C# files.

## 3. Problem

The previous approach treated JSON as a command language for Selenium.

Example:

```json
{
  "step": 1,
  "action": "click",
  "label": "Submit"
}
```

That approach creates several problems:

- Every new Selenium operation requires new runner code.
- Complex logic such as waits, conditions, loops, and retries becomes hard to express.
- JSON starts to become a second programming language.
- Step numbers are fragile when steps are added, deleted, or reordered.
- Element identification through labels is less reliable than Selenium locators.

The new model avoids those problems by keeping Selenium commands inside Selenium C# files.

## 4. Proposed Solution

Use JSON only as a file config.

Example:

```json
{
  "api": "CreateCase",
  "testSteps": [
    {
      "name": "Login",
      "description": "Log in to the application",
      "seleniumScript": "SeleniumScripts/Login.cs"
    },
    {
      "name": "Create Case",
      "description": "Create a new case",
      "dependency": "Login",
      "seleniumScript": "SeleniumScripts/CreateCase.cs"
    },
    {
      "name": "Submit Case",
      "description": "Submit the created case",
      "dependency": "Create Case",
      "seleniumScript": "SeleniumScripts/SubmitCase.cs"
    }
  ]
}
```

The application reads the config and executes the listed Selenium files in array order. The order of `testSteps` is the execution order. Numeric step fields are not required. If a step has `dependency`, it runs only when the named earlier step completed with `Success`; otherwise the dependent step is not executed and its result records which dependency failed, such as `Step 2 failed`.

## 5. Current Data Model

The file config should align with the current project model.

```csharp
public class FileConfig
{
    public string Api { get; init; } = string.Empty;

    public List<TestStep> TestSteps { get; init; } = new();
}
```

```csharp
public class TestStep
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Dependency { get; init; }

    public string SeleniumScript { get; init; } = string.Empty;
}
```

Future fields can be added when needed, but the MVP should keep this structure small and predictable.

## 6. Goals

The system must:

- Read a JSON file config.
- Identify the API or target flow from the config.
- Read the ordered `testSteps` list.
- Call each step's Selenium C# file in order.
- Run dependent steps only when the named earlier step succeeded.
- Use Selenium files for all browser commands.
- Reuse the same browser session across steps in one run.
- Record whether each step passed or failed.
- Stop or continue after failure based on future configuration.
- Capture useful logs and errors.
- Support screenshots on failure.
- Make test flows easy to add, remove, and reorder by editing JSON.

## 7. Non-Goals

The application should not recreate Selenium as JSON actions.

The file config should not contain browser commands like:

```json
{
  "action": "click"
}
```

```json
{
  "action": "wait"
}
```

```json
{
  "action": "findElement"
}
```

Those commands belong inside the Selenium files.

## 8. System Architecture

```text
File Config JSON
       |
       v
FileConfig
       |
       v
TestSteps
       |
       v
Selenium Handler
       |
       +-- WebDriver session
       +-- Step execution
       +-- Logging
       +-- Screenshot capture
       |
       v
Selenium C# Files
       |
       v
Browser
```

## 9. Recommended Project Structure

```text
Selenium Handler/
|
+-- FileConfig/
|   +-- CreateCase.json
|   +-- LoginFlow.json
|
+-- SeleniumScripts/
|   +-- Login.cs
|   +-- CreateCase.cs
|   +-- SubmitCase.cs
|
+-- Logs/
|
+-- Screenshots/
```

The exact folder names can follow the existing project folders, but the relationship should stay the same: JSON configs list steps, and steps reference Selenium files.

## 10. Selenium File Responsibility

Each Selenium file owns the actual Selenium automation for one step.

Example:

```csharp
public bool Run(IWebDriver driver)
{
    driver.FindElement(By.Id("username")).SendKeys("testuser");
    driver.FindElement(By.Id("password")).SendKeys("password");
    driver.FindElement(By.Id("login")).Click();

    return driver.FindElements(By.Id("dashboard")).Any();
}
```

The Selenium file may use normal Selenium features:

- `FindElement`
- `FindElements`
- `By.Id`
- `By.Name`
- `By.CssSelector`
- `By.XPath`
- `WebDriverWait`
- JavaScript execution
- screenshots
- conditional logic
- loops
- helper methods

Adding a new Selenium operation should normally require editing only a Selenium file, not the handler.

## 11. Execution Flow

1. User selects or provides a file config JSON.
2. The application deserializes the JSON into `FileConfig`.
3. The application validates required fields.
4. The application opens or reuses a WebDriver session.
5. The application runs each `TestStep` in order.
6. For each step, the application calls the referenced Selenium file.
7. The application records the result.
8. If a step fails, the application logs the failure and captures a screenshot.
9. The application returns the full run result.

## 12. Validation Rules

The application should validate:

- `api` is present.
- `testSteps` is present and contains at least one step.
- Each step has a `name`.
- Each step has a `seleniumScript`.
- Step names are unique inside one file config.
- A step `dependency`, when present, references another earlier step name.
- The referenced Selenium file exists.
- The referenced Selenium file can be called by the runner.

Validation errors should be reported before browser execution starts when possible.

## 13. Step Result

For MVP, each Selenium file should return a boolean result:

- `true` means the step passed.
- `false` means the step failed.
- An exception means the step failed with an error.

Future versions can support a richer result:

```csharp
public class ScriptResult
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public object? Data { get; set; }
}
```

## 14. Shared Browser Session

All steps in one config run should share the same WebDriver session.

Example:

```text
Login.cs
   |
   v
CreateCase.cs
   |
   v
SubmitCase.cs
```

`CreateCase.cs` should continue from the browser state created by `Login.cs`.

## 15. Logging

The runner should log:

- config file name
- API name
- run start time
- run end time
- each step name
- each Selenium script path
- step start time
- step end time
- pass/fail status
- exception details when a step fails
- screenshot path when a screenshot is captured

Example:

```text
START API CreateCase
START STEP Login
PASS STEP Login
START STEP Create Case
FAIL STEP Create Case
Screenshot: Screenshots/2026-09-19_135402_CreateCase_FAILED.png
END API CreateCase
```

## 16. Failure Handling

When a Selenium file returns `false` or throws an exception, the application should:

- mark the step as failed
- log the failure
- capture a screenshot if the browser is available
- include the script path in the failure result
- stop the run for MVP

Future configuration can add:

```json
{
  "stopOnFailure": true
}
```

## 17. Screenshot Support

The runner should automatically capture screenshots on failed steps.

Recommended naming:

```text
2026-09-19_135402_CreateCase_FAILED.png
```

Screenshots should be saved in a known screenshots folder and linked from the log or result.

## 18. Result Storage

The application should persist run results in SQL Server.

Database name:

```text
seleniumhandlerdocker
```

`TestResult` represents one config run.

```text
TestResult
- Id
- Name
- Description
- FilePath
- CreatedAt
```

`StepResult` represents the result of one step inside a config run.

```text
StepResult
- Id
- TestResultId
- Step
- Name
- Description
- Screenshot
- Result
- CreatedAt
```

`StepResult.TestResultId` must reference `TestResult.Id`. Deleting a test result should delete its step results.
The API must create the `TestResult` row and all planned `StepResult` rows at the start of a test run. Each step starts with `Result = Not Run`. Executed steps are updated to `Success` or `Error`. If a step is skipped because its dependency failed, its result should identify the dependency step, such as `Step 2 failed`. If a step fails, the runner should stop the current test, leave later steps as `Not Run`, close that browser, and continue with the next requested test config.

The basic relationship is:

```text
TestResult
   |
   +-- StepResult
   +-- StepResult
   +-- StepResult
```

For MVP, `Result` can be a string value such as:

- `Not Run`
- `Success`
- `Error`

Summary DTOs should label the test result as:

- `Not Run` when no step has run yet or remaining steps are still not run and no failure exists
- `Failure` when any step has `Error`
- `Success` when all steps completed successfully

## 19. Element Picker

The application may provide an element picker to help create Selenium files faster.

The picker should allow a user to:

- open a target page
- hover over an element
- highlight the hovered element
- click to select the element
- view available locator information
- copy generated Selenium C# code

The picker is a helper for writing Selenium files. It does not change the file config model.

## 20. Locator Generation

Generated locators should prefer stable selectors:

1. unique ID
2. stable application-specific attributes
3. name
4. stable CSS selector
5. XPath

Generated selectors should avoid fragile absolute paths when a simpler selector is available.

## 21. MVP Scope

The first version should focus on:

1. JSON file config loading.
2. `FileConfig` and `TestStep` model support.
3. Ordered test step execution.
4. Calling Selenium C# files from each step.
5. Shared Chrome WebDriver session.
6. Boolean pass/fail result per step.
7. Failure logging.
8. Automatic screenshots on failure.
9. SQL Server result tables for test and step results.

Element picker and generated Selenium code are valuable, but they can be built after the config-to-script execution path works.

## 22. Future Features

Future versions may add:

- Edge and Firefox support
- headless execution
- configurable timeout
- configurable stop-on-failure
- shared data between steps
- richer script result objects
- HTML reports
- script editor
- element picker
- Selenium code generation
- retry policies
- secrets management
- scheduled execution
- CI/CD support

## 23. Success Criteria

The project is successful when a developer can:

1. Create a JSON file config.
2. Add basic API information.
3. List ordered test steps.
4. Point each step to a Selenium C# file.
5. Run the config.
6. Let the application call each Selenium file in order.
7. Receive pass/fail results.
8. Receive logs and screenshots when a step fails.
9. Review saved test and step results from SQL Server.

The key rule is simple: JSON controls the flow; Selenium files control the browser.
