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

Write-Host "Running configured checks..."

if (Test-Path -LiteralPath "package.json") {
    if (Test-CommandAvailable "npm") {
        foreach ($script in @("check", "lint", "typecheck", "build")) {
            if (Test-NpmScript $script) {
                npm run $script
                if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
                $ran = $true
            }
        }
    } else {
        Write-Host "package.json found, but npm is not installed."
    }
}

$solutions = @(Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '\.(sln|slnx)$' } |
    Sort-Object FullName)
$projectFiles = @(Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -Filter "*.csproj" -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch 'Tests?\.csproj$' } |
    Sort-Object FullName)
$buildTargets = @()
if ($solutions.Count -gt 0) {
    $buildTargets = $solutions
} elseif ($projectFiles.Count -gt 0) {
    $buildTargets = $projectFiles
}

if ($buildTargets.Count -gt 0) {
    if (Test-CommandAvailable "dotnet") {
        foreach ($target in $buildTargets) {
            dotnet build $target.FullName -nologo -v:minimal -p:UseSharedCompilation=false
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            $ran = $true
        }
    } else {
        Write-Host ".NET project found, but dotnet is not installed."
    }
}

if ((Test-Path -LiteralPath "pyproject.toml") -or (Test-Path -LiteralPath "setup.py")) {
    if (Test-CommandAvailable "python") {
        if (Test-Path -LiteralPath "src") {
            python -m compileall src
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            $ran = $true
        } else {
            Write-Host "Python config found, but no src directory was detected for compile checks."
        }
    } else {
        Write-Host "Python config found, but python is not installed."
    }
}

$unityFiles = @(Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -Filter ProjectVersion.txt -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '[\\/]ProjectSettings[\\/]ProjectVersion\.txt$' })

if ($unityFiles.Count -gt 0) {
    Write-Host "Unity project detected. No Unity batchmode check is configured by this harness."
}

if (-not $ran) {
    Write-Host "No configured automated checks were detected."
}
