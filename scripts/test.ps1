$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $root = git rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -eq 0 -and $root) { return $root.Trim() }
    return (Get-Location).Path
}

function Test-CommandAvailable {
    param([string] $Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Test-NpmScript {
    param([string] $Name)
    if (-not (Test-Path -LiteralPath "package.json")) { return $false }
    $json = Get-Content -LiteralPath "package.json" -Raw | ConvertFrom-Json
    if (-not $json.scripts) { return $false }
    return $json.scripts.PSObject.Properties.Name -contains $Name
}

$root = Get-RepoRoot
Set-Location $root
$ran = $false

Write-Host "Running configured tests..."

if ((Test-Path -LiteralPath "package.json") -and (Test-CommandAvailable "npm") -and (Test-NpmScript "test")) {
    npm test
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $ran = $true
}

if ((Test-Path -LiteralPath "pytest.ini") -or (Test-Path -LiteralPath "pyproject.toml")) {
    if (Test-CommandAvailable "python") {
        python -m pytest
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        $ran = $true
    } else {
        Write-Host "Python test config found, but python is not installed."
    }
}

$testProjects = @(Get-ChildItem -LiteralPath $root -Recurse -Depth 4 -Filter "*Tests*.csproj" -File -ErrorAction SilentlyContinue |
    Sort-Object FullName)

if ($testProjects.Count -gt 0) {
    if (Test-CommandAvailable "dotnet") {
        foreach ($project in $testProjects) {
            dotnet test $project.FullName -nologo -v:minimal -p:UseSharedCompilation=false
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            $ran = $true
        }
    } else {
        Write-Host "Test project found, but dotnet is not installed."
    }
}

$unityFiles = @(Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -Filter ProjectVersion.txt -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '[\\/]ProjectSettings[\\/]ProjectVersion\.txt$' })

if ($unityFiles.Count -gt 0) {
    Write-Host "Unity project detected. No Unity batchmode tests are configured by this harness."
}

if (-not $ran) {
    Write-Host "No configured automated tests were detected."
}
