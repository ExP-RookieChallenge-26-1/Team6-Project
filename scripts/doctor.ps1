$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $root = git rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -eq 0 -and $root) { return $root.Trim() }
    return (Get-Location).Path
}

function Format-RelativePath {
    param([string] $Path, [string] $Root)
    $full = [System.IO.Path]::GetFullPath($Path)
    $rootFull = [System.IO.Path]::GetFullPath($Root)
    if ($full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = $full.Substring($rootFull.Length).TrimStart([char[]]@('\', '/'))
        if ([string]::IsNullOrWhiteSpace($relative)) { return "." }
        return $relative
    }
    return $full
}

$root = Get-RepoRoot
Set-Location $root

Write-Host "Codex harness doctor"
Write-Host "Root: $root"
Write-Host "Current directory: $(Get-Location)"
Write-Host ""

if (Get-Command git -ErrorAction SilentlyContinue) {
    Write-Host "Git status:"
    git status --short
} else {
    Write-Host "git: not installed"
}

Write-Host ""
Write-Host "Detected project files:"
$found = $false

foreach ($path in @("package.json", "pyproject.toml", "pytest.ini", "requirements.txt")) {
    if (Test-Path -LiteralPath $path) {
        Write-Host " - $path"
        $found = $true
    }
}

$projectFiles = Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '\.(sln|slnx|csproj)$' } |
    Sort-Object FullName

foreach ($file in $projectFiles) {
    Write-Host " - $(Format-RelativePath $file.FullName $root)"
    $found = $true
}

$unityFiles = Get-ChildItem -LiteralPath $root -Recurse -Depth 3 -Filter ProjectVersion.txt -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '[\\/]ProjectSettings[\\/]ProjectVersion\.txt$' } |
    Sort-Object FullName

foreach ($file in $unityFiles) {
    $projectDir = Split-Path (Split-Path $file.FullName -Parent) -Parent
    Write-Host " - Unity project: $(Format-RelativePath $projectDir $root)"
    $found = $true
}

if (-not $found) {
    Write-Host " - none of the common stack markers were found"
}
