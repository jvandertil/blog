@echo off
powershell -ExecutionPolicy ByPass -NoProfile -File .\hugo.ps1 serve -DEF --minify
exit /b %ERRORLEVEL%
