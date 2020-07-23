@echo off
powershell -ExecutionPolicy ByPass -NoProfile -File ..\src\blog\hugo.ps1 serve -DEF --minify --source ..\src\blog
exit /b %ERRORLEVEL%
