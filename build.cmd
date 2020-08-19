@echo off

del /s /f /Q %~dp0\artifacts

powershell -ExecutionPolicy ByPass -NoProfile -File %~dp0\eng\build-blog.ps1
powershell -ExecutionPolicy ByPass -NoProfile -File %~dp0\eng\build-uploader.ps1
powershell -ExecutionPolicy ByPass -NoProfile -File %~dp0\eng\build-infra.ps1
powershell -ExecutionPolicy ByPass -NoProfile -File %~dp0\eng\build-comment-function.ps1
