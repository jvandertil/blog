@echo off
powershell -ExecutionPolicy ByPass -NoProfile -File .\eng\build-blog.ps1
powershell -ExecutionPolicy ByPass -NoProfile -File .\eng\build-uploader.ps1
powershell -ExecutionPolicy ByPass -NoProfile -File .\eng\build-infra.ps1

