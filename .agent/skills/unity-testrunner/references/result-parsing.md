# Unity Test Result Parsing

NUnit XML result format parsing and CI/CD reporting integration.

## NUnit XML Result Structure

### Root Element

```xml
<?xml version="1.0" encoding="utf-8"?>
<test-run
    id="0"
    name="Assembly-CSharp.Tests"
    fullname="Assembly-CSharp.Tests"
    runstate="Runnable"
    testcasecount="25"
    result="Passed"
    total="25"
    passed="23"
    failed="1"
    inconclusive="0"
    skipped="1"
    asserts="48"
    engine-version="3.5.0.0"
    start-time="2024-01-15 10:30:00Z"
    end-time="2024-01-15 10:30:05Z"
    duration="5.234">
    <!-- test-suite and test-case elements -->
</test-run>
```

### Test Suite Element

```xml
<test-suite
    type="Assembly"
    name="Game.Domain.Tests"
    fullname="Game.Domain.Tests"
    classname=""
    runstate="Runnable"
    testcasecount="10"
    result="Passed"
    total="10"
    passed="10"
    failed="0"
    skipped="0"
    duration="1.234">
    <test-suite type="TestFixture" name="PlayerServiceTests">
        <!-- test-case elements -->
    </test-suite>
</test-suite>
```

### Test Case Element (Passed)

```xml
<test-case
    name="Initialize_WhenCalled_SetsHealthToMax"
    fullname="Game.Domain.Tests.PlayerServiceTests.Initialize_WhenCalled_SetsHealthToMax"
    methodname="Initialize_WhenCalled_SetsHealthToMax"
    classname="Game.Domain.Tests.PlayerServiceTests"
    runstate="Runnable"
    result="Passed"
    duration="0.012"
    asserts="1">
</test-case>
```

### Test Case Element (Failed)

```xml
<test-case
    name="Login_WithInvalidCredentials_ThrowsException"
    fullname="Game.Domain.Tests.AuthTests.Login_WithInvalidCredentials_ThrowsException"
    result="Failed"
    duration="0.045"
    asserts="1">
    <failure>
        <message><![CDATA[Expected: <System.ArgumentException>
  But was:  no exception thrown]]></message>
        <stack-trace><![CDATA[at Game.Domain.Tests.AuthTests.Login_WithInvalidCredentials_ThrowsException() in AuthTests.cs:line 42]]></stack-trace>
    </failure>
</test-case>
```

### Test Case Element (Skipped)

```xml
<test-case
    name="NetworkTest_RequiresConnection"
    fullname="Game.Tests.NetworkTests.NetworkTest_RequiresConnection"
    result="Skipped"
    duration="0.001">
    <reason>
        <message><![CDATA[No network connection available]]></message>
    </reason>
</test-case>
```

## PowerShell Parsing Scripts

### Basic Summary Extraction

```powershell
function Get-TestSummary {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ResultsPath
    )

    if (-not (Test-Path $ResultsPath)) {
        Write-Error "Results file not found: $ResultsPath"
        return $null
    }

    [xml]$results = Get-Content $ResultsPath
    $testRun = $results.'test-run'

    return [PSCustomObject]@{
        Total     = [int]$testRun.total
        Passed    = [int]$testRun.passed
        Failed    = [int]$testRun.failed
        Skipped   = [int]$testRun.skipped
        Duration  = [float]$testRun.duration
        Result    = $testRun.result
        StartTime = $testRun.'start-time'
        EndTime   = $testRun.'end-time'
    }
}

# Usage
$summary = Get-TestSummary -ResultsPath "results.xml"
Write-Host "Total: $($summary.Total), Passed: $($summary.Passed), Failed: $($summary.Failed)"
```

### Failed Test Extraction

```powershell
function Get-FailedTests {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ResultsPath
    )

    [xml]$results = Get-Content $ResultsPath

    $failedTests = $results.SelectNodes("//test-case[@result='Failed']")

    $failures = @()
    foreach ($test in $failedTests) {
        $failures += [PSCustomObject]@{
            Name       = $test.name
            FullName   = $test.fullname
            Duration   = [float]$test.duration
            Message    = $test.failure.message.'#cdata-section'
            StackTrace = $test.failure.'stack-trace'.'#cdata-section'
        }
    }

    return $failures
}

# Usage
$failures = Get-FailedTests -ResultsPath "results.xml"
foreach ($f in $failures) {
    Write-Host "`nFailed: $($f.Name)"
    Write-Host "Message: $($f.Message)"
}
```

### Complete Report Generator

```powershell
function Format-TestReport {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ResultsPath,

        [string]$TestPlatform = "EditMode"
    )

    $summary = Get-TestSummary -ResultsPath $ResultsPath
    $failures = Get-FailedTests -ResultsPath $ResultsPath

    $report = @"
Unity Test Results ($TestPlatform)
-----------------------------------
Passed:   $($summary.Passed)
Failed:   $($summary.Failed)
Skipped:  $($summary.Skipped)
Duration: $([math]::Round($summary.Duration, 2))s
"@

    if ($failures.Count -gt 0) {
        $report += "`n`nFailed Tests:`n"

        $i = 1
        foreach ($f in $failures) {
            $shortMessage = if ($f.Message.Length -gt 100) {
                $f.Message.Substring(0, 100) + "..."
            } else {
                $f.Message
            }

            $report += @"

  $i. $($f.Name)
     -> $($shortMessage.Trim())
"@
            $i++
        }
    }

    return $report
}

# Usage
$report = Format-TestReport -ResultsPath "results.xml" -TestPlatform "EditMode"
Write-Host $report
```

### Skipped Test Extraction

```powershell
function Get-SkippedTests {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ResultsPath
    )

    [xml]$results = Get-Content $ResultsPath

    $skippedTests = $results.SelectNodes("//test-case[@result='Skipped']")

    $skipped = @()
    foreach ($test in $skippedTests) {
        $skipped += [PSCustomObject]@{
            Name   = $test.name
            Reason = $test.reason.message.'#cdata-section'
        }
    }

    return $skipped
}
```

### Test Duration Analysis

```powershell
function Get-SlowestTests {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ResultsPath,

        [int]$Top = 10
    )

    [xml]$results = Get-Content $ResultsPath

    $allTests = $results.SelectNodes("//test-case")

    $sorted = $allTests | ForEach-Object {
        [PSCustomObject]@{
            Name     = $_.name
            Duration = [float]$_.duration
            Result   = $_.result
        }
    } | Sort-Object -Property Duration -Descending | Select-Object -First $Top

    return $sorted
}

# Usage
$slowest = Get-SlowestTests -ResultsPath "results.xml" -Top 5
$slowest | Format-Table -AutoSize
```

## CI/CD Integration

### GitHub Actions Annotation

```powershell
function Write-GitHubAnnotations {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ResultsPath
    )

    $failures = Get-FailedTests -ResultsPath $ResultsPath

    foreach ($f in $failures) {
        # Extract file and line from stack trace
        if ($f.StackTrace -match '([^\s]+\.cs):line (\d+)') {
            $file = $Matches[1]
            $line = $Matches[2]
            $message = $f.Message -replace "`n", " "

            Write-Host "::error file=$file,line=$line::$($f.Name): $message"
        } else {
            Write-Host "::error::$($f.Name): $($f.Message)"
        }
    }
}
```

### JUnit Format Conversion

Unity's NUnit format is mostly compatible with JUnit reporters. For tools requiring strict JUnit format:

```powershell
function Convert-ToJUnit {
    param(
        [Parameter(Mandatory=$true)]
        [string]$InputPath,

        [Parameter(Mandatory=$true)]
        [string]$OutputPath
    )

    [xml]$nunit = Get-Content $InputPath

    $junit = [xml]@"
<?xml version="1.0" encoding="utf-8"?>
<testsuites>
</testsuites>
"@

    $testRun = $nunit.'test-run'

    $testsuite = $junit.CreateElement("testsuite")
    $testsuite.SetAttribute("name", $testRun.name)
    $testsuite.SetAttribute("tests", $testRun.total)
    $testsuite.SetAttribute("failures", $testRun.failed)
    $testsuite.SetAttribute("skipped", $testRun.skipped)
    $testsuite.SetAttribute("time", $testRun.duration)

    $testCases = $nunit.SelectNodes("//test-case")

    foreach ($tc in $testCases) {
        $testcase = $junit.CreateElement("testcase")
        $testcase.SetAttribute("name", $tc.name)
        $testcase.SetAttribute("classname", $tc.classname)
        $testcase.SetAttribute("time", $tc.duration)

        if ($tc.result -eq "Failed") {
            $failure = $junit.CreateElement("failure")
            $failure.SetAttribute("message", $tc.failure.message.'#cdata-section')
            $failure.InnerText = $tc.failure.'stack-trace'.'#cdata-section'
            $testcase.AppendChild($failure) | Out-Null
        }
        elseif ($tc.result -eq "Skipped") {
            $skipped = $junit.CreateElement("skipped")
            $skipped.SetAttribute("message", $tc.reason.message.'#cdata-section')
            $testcase.AppendChild($skipped) | Out-Null
        }

        $testsuite.AppendChild($testcase) | Out-Null
    }

    $junit.testsuites.AppendChild($testsuite) | Out-Null
    $junit.Save($OutputPath)
}
```

### Exit Code Handler

```powershell
function Invoke-TestsWithReporting {
    param(
        [Parameter(Mandatory=$true)]
        [string]$UnityExe,

        [Parameter(Mandatory=$true)]
        [string]$ProjectPath,

        [string]$TestPlatform = "editmode",
        [string]$ResultsPath = "$env:TEMP\unity-test-results.xml"
    )

    # Run tests
    & $UnityExe -batchmode -nographics -projectPath $ProjectPath `
        -runTests -testPlatform $TestPlatform `
        -testResults $ResultsPath -quit

    $exitCode = $LASTEXITCODE

    # Generate report
    if (Test-Path $ResultsPath) {
        $report = Format-TestReport -ResultsPath $ResultsPath -TestPlatform $TestPlatform
        Write-Host $report

        # CI annotations
        if ($env:GITHUB_ACTIONS) {
            Write-GitHubAnnotations -ResultsPath $ResultsPath
        }
    }
    else {
        Write-Error "Test results not found. Check Unity log for errors."
    }

    # Return appropriate exit code
    switch ($exitCode) {
        0 { return 0 }                    # All passed
        2 { return 1 }                    # Test failures (CI failure)
        default { return $exitCode }      # Other errors
    }
}
```

## Output Format Templates

### Markdown Report

```powershell
function Format-MarkdownReport {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ResultsPath
    )

    $summary = Get-TestSummary -ResultsPath $ResultsPath
    $failures = Get-FailedTests -ResultsPath $ResultsPath

    $statusEmoji = if ($summary.Failed -eq 0) { "check" } else { "x" }

    $md = @"
## Test Results :$statusEmoji:

| Metric | Value |
|--------|-------|
| Total | $($summary.Total) |
| Passed | $($summary.Passed) |
| Failed | $($summary.Failed) |
| Skipped | $($summary.Skipped) |
| Duration | $([math]::Round($summary.Duration, 2))s |

"@

    if ($failures.Count -gt 0) {
        $md += "`n### Failed Tests`n`n"

        foreach ($f in $failures) {
            $md += @"
<details>
<summary>:x: $($f.Name)</summary>

``````
$($f.Message)
``````

**Stack Trace:**
``````
$($f.StackTrace)
``````

</details>

"@
        }
    }

    return $md
}
```

### JSON Report

```powershell
function Format-JsonReport {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ResultsPath
    )

    $summary = Get-TestSummary -ResultsPath $ResultsPath
    $failures = Get-FailedTests -ResultsPath $ResultsPath

    $report = @{
        summary = @{
            total    = $summary.Total
            passed   = $summary.Passed
            failed   = $summary.Failed
            skipped  = $summary.Skipped
            duration = $summary.Duration
            result   = $summary.Result
        }
        failures = @($failures | ForEach-Object {
            @{
                name       = $_.Name
                fullName   = $_.FullName
                message    = $_.Message
                stackTrace = $_.StackTrace
            }
        })
    }

    return $report | ConvertTo-Json -Depth 4
}
```

## Troubleshooting

### Empty Results File

```powershell
# Check if Unity completed successfully
if ((Get-Item $ResultsPath).Length -eq 0) {
    Write-Error "Empty results file. Check Unity log:"
    Get-Content $LogPath | Select-Object -Last 50
}
```

### Parse Errors

```powershell
# Handle malformed XML
try {
    [xml]$results = Get-Content $ResultsPath
}
catch {
    Write-Error "Failed to parse results XML: $_"
    Write-Host "Raw content:"
    Get-Content $ResultsPath
}
```

### Missing Test Cases

```powershell
# Verify test discovery
$testCount = $results.SelectNodes("//test-case").Count
if ($testCount -eq 0) {
    Write-Warning "No test cases found. Check:"
    Write-Warning "  1. Test assembly references"
    Write-Warning "  2. UNITY_INCLUDE_TESTS define"
    Write-Warning "  3. asmdef configuration"
}
```
