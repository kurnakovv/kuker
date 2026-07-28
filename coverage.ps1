$solutionDir = $PSScriptRoot
$testProjects = @(
    "tests\Kuker.Analyzers.Tests\Kuker.Analyzers.Tests.csproj",
    "tests\Kuker.CodeFixes.Tests\Kuker.CodeFixes.Tests.csproj"
)
$reportDir = "$solutionDir\coverage-report"

Write-Host "Running tests with coverage..." -ForegroundColor Cyan

foreach ($project in $testProjects) {
    $projectPath = Join-Path $solutionDir $project
    Write-Host "Testing: $project" -ForegroundColor Yellow
    dotnet test $projectPath --no-build --verbosity normal --collect:"XPlat Code Coverage"
}

Write-Host "Collecting coverage files..." -ForegroundColor Cyan

$xmlFiles = Get-ChildItem -Path $solutionDir\tests -Recurse -Filter "coverage.cobertura.xml" |
    Sort-Object LastWriteTime -Descending |
    Group-Object { $_.FullName -replace "\\TestResults\\.*", "" } |
    ForEach-Object { $_.Group | Select-Object -First 1 }

if (-not $xmlFiles) {
    Write-Host "No coverage files found!" -ForegroundColor Red
    exit 1
}

$reports = ($xmlFiles.FullName) -join ";"
Write-Host "Found coverage files:" -ForegroundColor Cyan
$xmlFiles.FullName | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }

Write-Host "Generating HTML report..." -ForegroundColor Cyan
reportgenerator -reports:$reports -targetdir:$reportDir -reporttypes:Html

Write-Host "Opening report..." -ForegroundColor Green
Start-Process "$reportDir\index.html"
