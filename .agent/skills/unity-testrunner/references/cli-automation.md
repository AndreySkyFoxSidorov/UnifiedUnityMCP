# Unity TestRunner CLI Automation

Complete reference for Unity Test Framework CLI automation, including path detection, execution options, and CI/CD integration.

## CLI Option Reference

### Required Options

| Option | Description |
|--------|-------------|
| `-runTests` | Enable test execution mode (required) |
| `-batchmode` | Run without GUI |
| `-projectPath` | Path to Unity project root |
| `-testPlatform` | `editmode`, `playmode`, or BuildTarget enum value |
| `-quit` | Exit Unity after test completion |

### Output Options

| Option | Description |
|--------|-------------|
| `-testResults` | Path for NUnit XML result file |
| `-logFile` | Path for Unity log output |

### Filter Options

| Option | Description | Example |
|--------|-------------|---------|
| `-testFilter` | Test name filter (semicolon-separated or regex) | `"LoginTest;AuthTest"` or `".*Service.*"` |
| `-testCategory` | Category filter (semicolon-separated) | `"Unit;Integration"` |
| `-assemblyNames` | Assembly filter (semicolon-separated) | `"Game.Domain.Tests"` |

### Performance Options

| Option | Description | Default |
|--------|-------------|---------|
| `-nographics` | Disable rendering (EditMode only) | - |
| `-playerHeartbeatTimeout` | Test timeout in seconds | 600 |

## Unity Hub Path Detection

### Windows

```powershell
function Get-UnityEditorPath {
    param([string]$ProjectPath)

    # Parse version from ProjectVersion.txt
    $versionPath = Join-Path $ProjectPath "ProjectSettings/ProjectVersion.txt"
    if (-not (Test-Path $versionPath)) {
        throw "ProjectVersion.txt not found. Verify Unity project path."
    }

    $content = Get-Content $versionPath
    $match = $content | Select-String "m_EditorVersion: (.+)"
    if (-not $match) {
        throw "Cannot parse Unity version from ProjectVersion.txt"
    }

    $version = $match.Matches.Groups[1].Value.Trim()

    # Try Unity Hub path
    $hubPath = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (Test-Path $hubPath) {
        return $hubPath
    }

    # Try environment variable
    if ($env:UNITY_EDITOR_PATH -and (Test-Path $env:UNITY_EDITOR_PATH)) {
        return $env:UNITY_EDITOR_PATH
    }

    throw "Unity $version not found. Set UNITY_EDITOR_PATH environment variable."
}
```

### macOS

```bash
get_unity_editor_path() {
    local project_path="$1"
    local version_file="$project_path/ProjectSettings/ProjectVersion.txt"

    if [[ ! -f "$version_file" ]]; then
        echo "Error: ProjectVersion.txt not found" >&2
        return 1
    fi

    local version=$(grep "m_EditorVersion:" "$version_file" | sed 's/m_EditorVersion: //')

    # Unity Hub path
    local hub_path="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity"
    if [[ -x "$hub_path" ]]; then
        echo "$hub_path"
        return 0
    fi

    # Environment variable fallback
    if [[ -n "$UNITY_EDITOR_PATH" && -x "$UNITY_EDITOR_PATH" ]]; then
        echo "$UNITY_EDITOR_PATH"
        return 0
    fi

    echo "Error: Unity $version not found" >&2
    return 1
}
```

## Complete Execution Scripts

### Windows PowerShell - EditMode

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,

    [string]$TestFilter = "",
    [string]$ResultsPath = "$env:TEMP\unity-test-results"
)

$ErrorActionPreference = "Stop"

# Ensure results directory exists
New-Item -ItemType Directory -Force -Path $ResultsPath | Out-Null

# Get Unity path
$unityExe = Get-UnityEditorPath -ProjectPath $ProjectPath

# Build command
$args = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "editmode",
    "-testResults", "$ResultsPath\editmode-results.xml",
    "-logFile", "$ResultsPath\editmode.log",
    "-quit"
)

if ($TestFilter) {
    $args += "-testFilter", $TestFilter
}

# Execute
Write-Host "Running EditMode tests..."
& $unityExe $args
$exitCode = $LASTEXITCODE

# Process results
switch ($exitCode) {
    0 { Write-Host "All tests passed" -ForegroundColor Green }
    2 { Write-Host "Some tests failed" -ForegroundColor Yellow }
    default { Write-Host "Error occurred (check log)" -ForegroundColor Red }
}

exit $exitCode
```

### Windows PowerShell - PlayMode

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,

    [string]$TestFilter = "",
    [string]$ResultsPath = "$env:TEMP\unity-test-results"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $ResultsPath | Out-Null

$unityExe = Get-UnityEditorPath -ProjectPath $ProjectPath

# Note: No -nographics for PlayMode
$args = @(
    "-batchmode",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "playmode",
    "-testResults", "$ResultsPath\playmode-results.xml",
    "-logFile", "$ResultsPath\playmode.log",
    "-quit"
)

if ($TestFilter) {
    $args += "-testFilter", $TestFilter
}

Write-Host "Running PlayMode tests..."
& $unityExe $args
$exitCode = $LASTEXITCODE

switch ($exitCode) {
    0 { Write-Host "All tests passed" -ForegroundColor Green }
    2 { Write-Host "Some tests failed" -ForegroundColor Yellow }
    default { Write-Host "Error occurred (check log)" -ForegroundColor Red }
}

exit $exitCode
```

## CI/CD Pipeline Templates

### GitHub Actions

```yaml
name: Unity Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true

      - uses: game-ci/unity-test-runner@v4
        id: tests
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
        with:
          projectPath: .
          testMode: editmode
          artifactsPath: TestResults
          githubToken: ${{ secrets.GITHUB_TOKEN }}

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: Test Results
          path: TestResults

      - name: Report Test Results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Unity Tests
          path: TestResults/*.xml
          reporter: java-junit
```

### GitHub Actions (Self-Hosted Windows Runner)

```yaml
name: Unity Tests (Self-Hosted)

on:
  push:
    branches: [main]

jobs:
  test:
    runs-on: self-hosted

    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true

      - name: Run EditMode Tests
        shell: powershell
        run: |
          $version = (Get-Content "ProjectSettings/ProjectVersion.txt" |
            Select-String "m_EditorVersion: (.+)").Matches.Groups[1].Value
          $unity = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"

          & $unity -batchmode -nographics -projectPath . `
            -runTests -testPlatform editmode `
            -testResults TestResults/editmode.xml `
            -logFile TestResults/editmode.log -quit

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: Test Results
          path: TestResults/
```

### GitLab CI

```yaml
stages:
  - test

variables:
  UNITY_VERSION: "6000.1.16f1"

editmode-tests:
  stage: test
  image: unityci/editor:ubuntu-${UNITY_VERSION}-linux-il2cpp-3
  script:
    - unity-editor -batchmode -nographics
        -projectPath .
        -runTests
        -testPlatform editmode
        -testResults results/editmode.xml
        -logFile results/editmode.log
        -quit
  artifacts:
    when: always
    paths:
      - results/
    reports:
      junit: results/*.xml
```

## Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `UNITY_EDITOR_PATH` | Direct path to Unity executable | `C:\Program Files\Unity\Hub\Editor\6000.1.16f1\Editor\Unity.exe` |
| `UNITY_LICENSE` | Base64-encoded Unity license (CI) | Used with game-ci actions |

## Timeout Configuration

### Default Timeouts

| Timeout | Default | Description |
|---------|---------|-------------|
| Player heartbeat | 600s | Max time between test events |
| Total execution | None | No overall limit by default |

### Setting Custom Timeout

```powershell
# 10-minute timeout
& $unityExe -batchmode -projectPath . -runTests `
  -testPlatform editmode `
  -playerHeartbeatTimeout 600 `
  -testResults results.xml -quit
```

## Log File Analysis

### Common Log Patterns

```powershell
# Check for test execution start
Select-String "Running tests" $logFile

# Find test failures
Select-String "FAILED:" $logFile

# Check for compilation errors
Select-String "error CS" $logFile

# Find async test issues
Select-String "Timeout" $logFile
```

### Extracting Error Context

```powershell
# Get 5 lines around each error
Select-String "error" $logFile -Context 2,3
```

## Troubleshooting

### Unity Not Found

```powershell
# Verify Unity installation
$version = "6000.1.16f1"
$paths = @(
    "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe",
    "C:\Program Files\Unity\$version\Editor\Unity.exe",
    "$env:UNITY_EDITOR_PATH"
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        Write-Host "Found: $path"
        break
    }
}
```

### License Issues

```powershell
# Check license status
& $unityExe -batchmode -quit -logFile license-check.log

# Activate license (CI)
& $unityExe -batchmode -quit -username $user -password $pass -serial $serial
```

### Test Not Found

```powershell
# List all test assemblies
Get-ChildItem -Recurse -Filter "*.Tests.asmdef" | ForEach-Object {
    Write-Host $_.FullName
}

# Verify test discovery
& $unityExe -batchmode -projectPath . -runTests -testPlatform editmode `
  -logFile discover.log -quit

Select-String "Discovered" discover.log
```
