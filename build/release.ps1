<#
.SYNOPSIS
    Compila, empaqueta (vpk) y publica una nueva versión de Lanzador en GitHub Releases.

.PARAMETER Version
    Versión a publicar, formato semver (ej: 1.0.1).

.PARAMETER RepoUrl
    URL del repositorio de GitHub (ej: https://github.com/tu-usuario/lanzador).
    Debe coincidir con AppSettings.UpdateRepoUrl en el código.

.PARAMETER GithubToken
    Token de GitHub con permiso "repo" (o "public_repo" si el repo es público) para subir la release.
    Puedes pasarlo aquí o dejar que lo lea de la variable de entorno GITHUB_TOKEN.

.EXAMPLE
    ./build/release.ps1 -Version 1.0.1 -RepoUrl https://github.com/tu-usuario/lanzador
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$RepoUrl,

    [string]$GithubToken = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\Lanzador\Lanzador.csproj"
$publishDir = Join-Path $root "build\publish"
$releasesDir = Join-Path $root "build\Releases"

if (-not $GithubToken) {
    Write-Warning "No se ha indicado un token de GitHub (-GithubToken o variable GITHUB_TOKEN). Si el repo es privado, la subida fallará."
}

Write-Host "==> Restaurando herramienta vpk..." -ForegroundColor Cyan
dotnet tool restore

Write-Host "==> Publicando build self-contained ($Version)..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $project -c Release -r win-x64 --self-contained true -p:Version=$Version -o $publishDir

Write-Host "==> Empaquetando con vpk..." -ForegroundColor Cyan
dotnet vpk pack `
    --packId "Lanzador" `
    --packTitle "Lanzador" `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe "Lanzador.exe" `
    --icon (Join-Path $root "assets\app.ico") `
    --outputDir $releasesDir `
    --shortcuts "StartMenuRoot"

Write-Host "==> Subiendo release a GitHub..." -ForegroundColor Cyan
$uploadArgs = @(
    "vpk", "upload", "github",
    "--repoUrl", $RepoUrl,
    "--outputDir", $releasesDir,
    "--publish"
)
if ($GithubToken) {
    $uploadArgs += @("--token", $GithubToken)
}

dotnet @uploadArgs

Write-Host "==> Listo. Versión $Version publicada en $RepoUrl" -ForegroundColor Green
