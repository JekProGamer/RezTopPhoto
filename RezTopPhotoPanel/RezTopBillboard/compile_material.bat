@echo off
setlocal

REM Edit CS2_ROOT if CS2 is installed in another Steam library.
set "CS2_ROOT=%ProgramFiles(x86)%\Steam\steamapps\common\Counter-Strike Global Offensive"
set "RESOURCECOMPILER=%CS2_ROOT%\game\bin\win64\resourcecompiler.exe"

REM Addon content destination.
set "ADDON_CONTENT=%CS2_ROOT%\content\csgo_addons\reztopphoto"
set "MATERIAL_DIR=%ADDON_CONTENT%\materials\reztopphoto"
set "VMAT=%MATERIAL_DIR%\reztop_billboard.vmat"

if not exist "%RESOURCECOMPILER%" (
  echo ERROR: resourcecompiler.exe not found:
  echo %RESOURCECOMPILER%
  echo Edit CS2_ROOT in this BAT file to your CS2 installation.
  pause
  exit /b 1
)

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

if not exist "%MATERIAL_DIR%" mkdir "%MATERIAL_DIR%"

copy /Y "%~dp0reztop_billboard.vmat" "%MATERIAL_DIR%\reztop_billboard.vmat" >nul
copy /Y "%~dp0reztop_billboard_texture.png" "%MATERIAL_DIR%\reztop_billboard_texture.png" >nul

echo Compiling RezTopPhoto billboard material...
"%RESOURCECOMPILER%" -i "%VMAT%"
if errorlevel 1 (
  echo Material compilation failed. Check the compiler output above.
  pause
  exit /b 1
)

echo.
echo Compiler finished. Check the addon game folder for compiled .vmat_c and .vtex_c files.
echo Next step is importing the OBJ mesh through Blender and ModelDoc.
pause
