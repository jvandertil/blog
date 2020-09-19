@echo off
powershell -ExecutionPolicy ByPass -NoProfile -File %~dp0\..\src\blog\hugo.ps1 serve -DEF --minify --source %~dp0\..\src\blog
exit /b %ERRORLEVEL%
