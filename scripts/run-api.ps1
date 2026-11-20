# Levanta la API en http://localhost:7100 con el entorno de desarrollo.
#
# Compila con un solo nodo de MSBuild (-m:1 -nodeReuse:false) para evitar
# MSB4166 ("el nodo secundario se cerró antes de tiempo"), que en equipos
# con recursos limitados hace que `dotnet run` termine sin levantar la API.
#
# Uso:  powershell -ExecutionPolicy Bypass -File scripts\run-api.ps1

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$api = Join-Path $root 'src\SistemaGestion.API'

Set-Location $api

Write-Host "==> Compilando API (MSBuild de un solo nodo)" -ForegroundColor Cyan
dotnet build -m:1 -nodeReuse:false -v:q
if ($LASTEXITCODE -ne 0) { throw "dotnet build falló" }

$env:ASPNETCORE_ENVIRONMENT = 'Development'

Write-Host "==> Iniciando API en http://localhost:7100" -ForegroundColor Cyan
dotnet run --no-build --no-launch-profile --urls "http://localhost:7100"
