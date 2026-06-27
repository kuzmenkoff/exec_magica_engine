$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$src  = Join-Path $root "Assets\_Project\Scripts\Gameplay\LogicLayer"
$dst  = Join-Path $PSScriptRoot "_src"

if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
Get-ChildItem $src -Recurse -Filter *.cs | ForEach-Object {
    $rel = $_.FullName.Substring($src.Length).TrimStart('\')
    $out = Join-Path $dst $rel
    New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null
    "namespace ExecMagica.Engine {`r`n" + (Get-Content $_.FullName -Raw) + "`r`n}" |
        Set-Content -Path $out -Encoding UTF8
}

Push-Location $PSScriptRoot
docfx metadata
# API landing page so /api/ resolves to an index.html on GitHub Pages
"# API Reference`r`n`r`nAll public types of the EXEC_MAGICA engine, AI agents and telemetry, under the ExecMagica.Engine namespace. Use the left sidebar or the search to browse." |
    Set-Content -Path (Join-Path $PSScriptRoot "api\index.md") -Encoding UTF8
docfx build
Pop-Location
Write-Host "Done -> docfx\_site  (preview: docfx serve docfx\_site)"