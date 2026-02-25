@echo off
setlocal

set "script_dir=%~dp0"
cd /d "%script_dir%"

set "rid=win-x64"
set "outdir=%script_dir%dist\%rid%"
set "debug_args=/p:DebugSymbols=false /p:DebugType=None"

:parse_args
if "%~1"=="" goto after_parse
if /I "%~1"=="--include-pdb" (
  set "debug_args="
  shift
  goto parse_args
)
echo Unknown option: %~1
echo Usage: %~nx0 [--include-pdb]
exit /b 1

:after_parse

echo Publishing Excel2DBSharp for %rid%...
dotnet publish Excel2DBSharp.csproj -c Release -r %rid% ^
  /p:PublishSingleFile=true ^
  /p:SelfContained=true ^
  /p:PublishTrimmed=true ^
  %debug_args% ^
  -o "%outdir%"

if errorlevel 1 exit /b %errorlevel%

echo.
echo Publish complete:
echo %outdir%\Excel2DBSharp.exe
