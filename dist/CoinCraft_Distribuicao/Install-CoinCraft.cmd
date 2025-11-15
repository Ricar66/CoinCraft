@echo off
setlocal
REM Wrapper para executar o instalador em um clique
set SCRIPT_DIR=%~dp0
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Install-CoinCraft.ps1" -CreateDesktopShortcut
if %ERRORLEVEL% NEQ 0 (
  echo Houve uma falha na instalacao. Tente executar como Administrador.
  pause
)
endlocal