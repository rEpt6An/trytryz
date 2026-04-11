@echo off
setlocal
cd /d "%~dp0"

REM Unity project folder name: trytryz (contains Assets\Design, Assets\Resources\...)
set "UNITY_RES=..\..\trytryz\Assets\Resources\Tables"
set "XLSX=..\..\trytryz\Assets\Design\items.xlsx"

if not exist "%XLSX%" (
  echo Excel not found: %XLSX%
  exit /b 1
)

python export_xlsx.py "%XLSX%" --format json --unity-json --sheets Sheet1 --json-basename items -o "%UNITY_RES%"
if errorlevel 1 (
  echo.
  echo If "python" was not found: install Python 3 from https://www.python.org/downloads/
  echo During setup, check "Add python.exe to PATH", then run: pip install -r requirements.txt
  exit /b 1
)
echo Exported JSON into: %UNITY_RES%
endlocal
