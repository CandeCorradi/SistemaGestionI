# Configura el entorno de desarrollo: restaura, compila y testea el backend;
# instala dependencias del frontend y ejecuta su build/lint.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "==> Restaurando solución backend" -ForegroundColor Cyan
# --disable-parallel evita MSB4166 ("el nodo secundario se cerró antes de
# tiempo") en equipos con recursos limitados.
dotnet restore SistemaGestion.sln --disable-parallel
if ($LASTEXITCODE -ne 0) { throw "dotnet restore falló" }

Write-Host "==> Compilando backend" -ForegroundColor Cyan
dotnet build SistemaGestion.sln --no-restore -m:1 -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw "dotnet build falló" }

Write-Host "==> Ejecutando tests backend" -ForegroundColor Cyan
dotnet test SistemaGestion.sln --no-build
if ($LASTEXITCODE -ne 0) { throw "dotnet test falló" }

if (Test-Path "$root\frontend\package.json") {
    Write-Host "==> Instalando dependencias del frontend" -ForegroundColor Cyan
    Push-Location "$root\frontend"
    try {
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install falló" }
    }
    finally { Pop-Location }
}

Write-Host "Setup completado." -ForegroundColor Green