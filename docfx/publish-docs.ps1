$ErrorActionPreference = "Stop"
$site = Join-Path $PSScriptRoot "_site"
$pub  = Join-Path $PSScriptRoot "_ghpages"
if (-not (Test-Path $site)) { throw "Run build-docs.ps1 first." }
if (-not (Test-Path $pub))  { throw "Create the gh-pages worktree first." }

robocopy $site $pub /MIR /XF .git | Out-Null
New-Item -ItemType File -Force -Path (Join-Path $pub ".nojekyll") | Out-Null

Push-Location $pub
git add -A
git commit -m "docs: update API reference" 2>$null
git push origin gh-pages
Pop-Location
Write-Host "Published -> gh-pages"