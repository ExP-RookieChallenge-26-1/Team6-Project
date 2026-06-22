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

$root = Get-RepoRoot
Set-Location $root
$ran = $false

Write-Host "Bootstrapping detected stacks..."

if (Test-Path -LiteralPath "package.json") {
    if (Test-CommandAvailable "npm") {
        if (Test-Path -LiteralPath "package-lock.json") {
            npm ci
        } else {
            npm install
        }
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        $ran = $true
    } else {
        Write-Host "package.json found, but npm is not installed."
    }
}

$solutions = @(Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '\.(sln|slnx)$' } |
    Sort-Object FullName)
$projectFiles = @(Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -Filter "*.csproj" -File -ErrorAction SilentlyContinue |
    Sort-Object FullName)
$buildTarget = $null
if ($solutions.Count -gt 0) {
    $buildTarget = $solutions[0]
} elseif ($projectFiles.Count -gt 0) {
    $buildTarget = $projectFiles[0]
}

if ($buildTarget) {
    if (Test-CommandAvailable "dotnet") {
        dotnet restore $buildTarget.FullName
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        $ran = $true
    } else {
        Write-Host ".NET project found, but dotnet is not installed."
    }
}

if (Test-Path -LiteralPath "requirements.txt") {
    if (Test-CommandAvailable "python") {
        python -m pip install -r requirements.txt
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        $ran = $true
    } else {
        Write-Host "requirements.txt found, but python is not installed."
    }
}

$unityFiles = @(Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -Filter ProjectVersion.txt -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '[\\/]ProjectSettings[\\/]ProjectVersion\.txt$' })

if ($unityFiles.Count -gt 0) {
    Write-Host "Unity project detected. Unity Package Manager dependencies are resolved by Unity Editor; no CLI install was assumed."
}

if (-not $ran) {
    Write-Host "No installable root dependency stack was detected."
}
