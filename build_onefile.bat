@echo off
SET batpath=%~dp0
cd %batpath%

REM Build Excel2DBSharp single-file EXE

dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true -p:PublishTrimmed=true -o dist

rmdir /s /q %batpath%\bin
rmdir /s /q %batpath%\obj

echo.
echo Build complete. EXE located in:
echo %~dp0dist\Excel2DBSharp.exe
