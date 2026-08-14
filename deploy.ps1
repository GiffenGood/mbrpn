# Deploys MB-RPN to GitHub Pages: test -> publish -> patch for Pages -> push gh-pages.
# Prerequisites (one-time): git remote "origin" pointing at the GitHub repo, and
# GitHub Pages enabled for the gh-pages branch (Settings -> Pages, or the gh api call
# in the README).

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$repoName = 'mbrpn'   # the site is served at https://<user>.github.io/mbrpn/
$publishRoot = Join-Path $PSScriptRoot 'src\MBRPN.Web\bin\Release\net10.0\publish\wwwroot'

Write-Host '=== Running tests ===' -ForegroundColor Cyan
dotnet test --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Tests failed - deploy aborted.' }

Write-Host '=== Publishing Release build ===' -ForegroundColor Cyan
dotnet publish src\MBRPN.Web -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Publish failed - deploy aborted.' }
if (-not (Test-Path (Join-Path $publishRoot 'index.html'))) { throw "Publish output not found at $publishRoot" }

Write-Host '=== Patching output for GitHub Pages ===' -ForegroundColor Cyan
# Project pages live under /<repo>/, so the deployed copy needs that base href.
$indexPath = Join-Path $publishRoot 'index.html'
$html = Get-Content $indexPath -Raw
$patched = $html -replace '<base href="/" />', ('<base href="/' + $repoName + '/" />')
if ($patched -eq $html -and $html -notmatch [regex]::Escape("/$repoName/")) {
    throw 'Could not find <base href="/" /> to rewrite - index.html layout changed?'
}
Set-Content -Path $indexPath -Value $patched -NoNewline

# .nojekyll stops Pages' Jekyll pass from dropping the _framework folder.
New-Item -ItemType File -Path (Join-Path $publishRoot '.nojekyll') -Force | Out-Null
# 404 fallback boots the app on a deep refresh.
Copy-Item $indexPath (Join-Path $publishRoot '404.html') -Force

Write-Host '=== Pushing to gh-pages ===' -ForegroundColor Cyan
$remote = git remote get-url origin
if ($LASTEXITCODE -ne 0 -or -not $remote) {
    throw 'No "origin" remote. Create the GitHub repo first (see README "Deploying").'
}

Push-Location $publishRoot
try {
    if (Test-Path '.git') { Remove-Item '.git' -Recurse -Force }
    git init -b gh-pages -q
    git add -A
    git -c user.name='MB-RPN deploy' -c user.email='deploy@mbrpn.local' commit -q -m 'Deploy MB-RPN to GitHub Pages'
    if ($LASTEXITCODE -ne 0) { throw 'Commit of publish output failed.' }
    git push --force $remote gh-pages
    if ($LASTEXITCODE -ne 0) { throw 'Push to gh-pages failed.' }
    Remove-Item '.git' -Recurse -Force
}
finally {
    Pop-Location
}

$owner = ($remote -replace '\.git$','' -split '[:/]')[-2]
Write-Host ''
Write-Host 'Deployed. The site updates in about a minute:' -ForegroundColor Green
Write-Host "  https://$owner.github.io/$repoName/" -ForegroundColor Green
