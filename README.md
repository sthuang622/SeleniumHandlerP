# Selenium Handler

See the [FileConfig, TestConfig, and Selenium script setup guide](docs/SCRIPT_SETUP.md) for configuration examples and instructions.

## Before the first launch

Start Docker Desktop, then run the following command from the repository root **before launching the application**. The Compose file is located at `docker/docker-compose.yml`:

```powershell
docker compose -f docker/docker-compose.yml up -d
```

This starts SQL Server and runs the database initialization script. Wait for the `seleniumhandler-sqlserver-init` container to finish successfully before launching Selenium Handler. You can check its status with `docker compose -f docker/docker-compose.yml ps -a` and its output with `docker compose -f docker/docker-compose.yml logs seleniumhandler-sqlserver-init`.

SQL Server must also be running on subsequent launches. Run `docker compose -f docker/docker-compose.yml up -d` again if the containers are stopped.

## About the project

Selenium Handler is a web-based Selenium test runner for executing browser automation from JSON configuration files. A file config can contain multiple test configs, and each test config contains ordered Selenium script steps. The app reads shared parameters from the JSON files, runs the matching C# Selenium scripts, supports step and test dependencies, and records the full run history.

The application stores file config results, test results, step results, screenshots, timestamps, and error messages in SQL Server. The web UI lets users select and run multiple configs, review grouped run results, expand test and step details, view screenshots, inspect failure messages, and download HTML reports with linked screenshots.

The project also includes Docker SQL Server setup scripts and public demo tests based on `the-internet.herokuapp.com`. The demo configs show successful runs, expected failures, syntax errors, missing elements, timeouts, skipped dependency behavior, dropdowns, dynamic controls, login/logout, and report generation without relying on private test systems.
