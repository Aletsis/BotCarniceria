# Script para generar reportes de cobertura de código
# Ejecutar desde la raíz del proyecto

Write-Host "=== Generando Reportes de Cobertura de Código ===" -ForegroundColor Cyan
Write-Host ""

# Limpiar resultados anteriores
Write-Host "Limpiando resultados anteriores..." -ForegroundColor Yellow
if (Test-Path "TestResults") {
    Remove-Item -Path "TestResults" -Recurse -Force
}

# Ejecutar tests unitarios con cobertura
Write-Host "Ejecutando tests unitarios..." -ForegroundColor Yellow
dotnet test tests/BotCarniceria.UnitTests/BotCarniceria.UnitTests.csproj `
    --collect:"XPlat Code Coverage" `
    --settings:"coverlet.runsettings" `
    --results-directory:"TestResults" `
    --verbosity:minimal

# Ejecutar tests de Presentation.API con cobertura
Write-Host "Ejecutando tests de Presentation.API..." -ForegroundColor Yellow
dotnet test tests/BotCarniceria.Presentation.API.Tests/BotCarniceria.Presentation.API.Tests.csproj `
    --collect:"XPlat Code Coverage" `
    --settings:"coverlet.runsettings" `
    --results-directory:"TestResults" `
    --verbosity:minimal

# Ejecutar tests de Presentation.Blazor con cobertura
Write-Host "Ejecutando tests de Presentation.Blazor..." -ForegroundColor Yellow
dotnet test tests/BotCarniceria.Presentation.Blazor.Tests/BotCarniceria.Presentation.Blazor.Tests.csproj `
    --collect:"XPlat Code Coverage" `
    --settings:"coverlet.runsettings" `
    --results-directory:"TestResults" `
    --verbosity:minimal

# Ejecutar tests de Application.Bot con cobertura
Write-Host "Ejecutando tests de Application.Bot..." -ForegroundColor Yellow
dotnet test tests/BotCarniceria.Application.Bot.Tests/BotCarniceria.Application.Bot.Tests.csproj `
    --collect:"XPlat Code Coverage" `
    --settings:"coverlet.runsettings" `
    --results-directory:"TestResults" `
    --verbosity:minimal

# Ejecutar tests de Infrastructure con cobertura
Write-Host "Ejecutando tests de Infrastructure..." -ForegroundColor Yellow
dotnet test tests/BotCarniceria.Infrastructure.Tests/BotCarniceria.Infrastructure.Tests.csproj `
    --collect:"XPlat Code Coverage" `
    --settings:"coverlet.runsettings" `
    --results-directory:"TestResults" `
    --verbosity:minimal

# Generar reporte HTML
Write-Host ""
Write-Host "Generando reporte HTML..." -ForegroundColor Yellow
reportgenerator `
    "-reports:TestResults/**/coverage.cobertura.xml" `
    "-targetdir:TestResults/CoverageReport" `
    "-reporttypes:Html;Cobertura;TextSummary;Badges" `
    "-filefilters:-*Migrations*;-*ModelSnapshot*;-*Designer.cs;-*Configuration*"

# Mostrar resumen
Write-Host ""
Write-Host "=== Resumen de Cobertura ===" -ForegroundColor Cyan
Get-Content "TestResults/CoverageReport/Summary.txt" | Select-Object -First 20

Write-Host ""
Write-Host "Reporte completo generado en: TestResults/CoverageReport/index.html" -ForegroundColor Green
Write-Host "Para ver el reporte, ejecuta: Start-Process TestResults/CoverageReport/index.html" -ForegroundColor Green
Write-Host ""
