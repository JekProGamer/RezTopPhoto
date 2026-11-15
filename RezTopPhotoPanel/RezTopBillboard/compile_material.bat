@echo off
setlocal

REM Edit this path if CS2 is installed in a non-default Steam library.
set "CS2_ROOT=%ProgramFiles(x86)%\Steam\steamapps\common\Counter-Strike Global Offensive"
set "RESOURCECOMPILER=%CS2_ROOT%\game\bin\win64\resourcecompiler.exe"

REM Expected addon content path:
REM content\csgo_addons\reztopphoto\materials\reztopphoto\reztop_billboard.vmat
set "VMAT=%~dp0reztop_billboard.vmat"

if not exist "%RESOURCECOMPILER%" (
  echo ERROR: resourcecompiler.exe not found:
  echo %RESOURCECOMPILER%
  echo Edit CS2_ROOT in this BAT file to your CS2 installation.
  pause
  exit /b 1
)

if not exist "%VMAT%" (
  echo ERROR: reztop_billboard.vmat not found next to this BAT file.
  pause
  exit /b 1
)

echo Compiling material source...
"%RESOURCECOMPILER%" -i "%VMAT%"
if errorlevel 1 (
  echo Material compilation failed. Check the compiler output above.
  pause
  exit /b 1
)

echo.
echo Compiler finished. Check for .vmat_c and .vtex_c outputs.
pause
