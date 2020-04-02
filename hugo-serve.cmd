@echo off
powershell -ExecutionPolicy ByPass -NoProfile -File .\hugo.ps1 serve -DEF
exit /b %ERRORLEVEL%
