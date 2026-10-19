# Selenium Handler

See the [FileConfig, TestConfig, and Selenium script setup guide](docs/SCRIPT_SETUP.md) for configuration examples and instructions.

Selenium Handler is a web-based Selenium test runner for executing browser automation from JSON configuration files. A file config can contain multiple test configs, and each test config contains ordered Selenium script steps. The app reads shared parameters from the JSON files, runs the matching C# Selenium scripts, supports step and test dependencies, and records the full run history.

The application stores file config results, test results, step results, screenshots, timestamps, and error messages in SQL Server. The web UI lets users select and run multiple configs, review grouped run results, expand test and step details, view screenshots, inspect failure messages, and download HTML reports with linked screenshots.

The project also includes Docker SQL Server setup scripts and public demo tests based on `the-internet.herokuapp.com`. The demo configs show successful runs, expected failures, syntax errors, missing elements, timeouts, skipped dependency behavior, dropdowns, dynamic controls, login/logout, and report generation without relying on private test systems.
