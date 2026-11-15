@echo off
setlocal
title RezTopPhoto - CS2 Material Compiler

REM CS2 installation path confirmed by the user.
set "CS2_ROOT=D:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive"
set "RESOURCECOMPILER=%CS2_ROOT%\game\bin\win64\resourcecompiler.exe"

REM Addon content destination.
set "ADDON_CONTENT=%CS2_ROOT%\content\csgo_addons\reztopphoto"
set "MATERIAL_DIR=%ADDON_CONTENT%\materials\reztopphoto"
set "VMAT=%MATERIAL_DIR%\reztop_billboard.vmat"

echo [1/4] Checking CS2 Resource Compiler...
if not exist "%RESOURCECOMPILER%" (
  echo ERROR: resourcecompiler.exe not found:
  echo "%RESOURCECOMPILER%"
  echo Check that CS2 Workshop Tools are installed and verify the CS2 path.
  pause
  exit /b 1
)

echo [2/4] Checking source files...
if not exist "%~dp0reztop_billboard.vmat" (
  echo ERROR: reztop_billboard.vmat not found next to this BAT file.
  pause
  exit /b 1
)
if not exist "%~dp0reztop_billboard_texture.png" (
  echo ERROR: reztop_billboard_texture.png not found next to this BAT file.
  pause
  exit /b 1
)

echo [3/4] Copying files into addon content...
if not exist "%MATERIAL_DIR%" mkdir "%MATERIAL_DIR%"
copy /Y "%~dp0reztop_billboard.vmat" "%MATERIAL_DIR%\reztop_billboard.vmat"
if errorlevel 1 (
  echo ERROR: Could not copy VMAT.
  pause
  exit /b 1
)
copy /Y "%~dp0reztop_billboard_texture.png" "%MATERIAL_DIR%\reztop_billboard_texture.png"
if errorlevel 1 (
  echo ERROR: Could not copy PNG texture.
  pause
  exit /b 1
)

echo [4/4] Compiling material...
"%RESOURCECOMPILER%" -i "%VMAT%"
if errorlevel 1 (
  echo ERROR: Material compilation failed. Read the compiler output above.
  pause
  exit /b 1
)

echo.
echo Resource Compiler finished. Check the addon output for compiled resources.
echo Material compilation is not the same as compiling the OBJ model.
pause
