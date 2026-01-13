@echo off
REM Build Excel2DBSharp single-file EXE

dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true -o dist

echo.
echo Build complete. EXE located in:
echo %~dp0dist\Excel2DBSharp.exe
pause
