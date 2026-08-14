@echo off
rem Deploys MB-RPN to GitHub Pages. Double-click me.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy.ps1"
pause
