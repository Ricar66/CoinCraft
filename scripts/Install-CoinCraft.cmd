@echo off
setlocal DisableDelayedExpansion
REM Instalador CoinCraft - único ponto de entrada (.cmd)
echo == CoinCraft Installer ==
echo Este instalador pode solicitar a selecao do arquivo .msix.

set "SCRIPT_DIR=%~dp0"
echo Executando: %SCRIPT_DIR%Install-CoinCraft.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Install-CoinCraft.ps1" -CreateDesktopShortcut

set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" goto :fail
goto :end

:fail
echo(
echo Houve uma falha na instalacao (codigo %EXITCODE%).
echo Se solicitado, execute como Administrador.
echo Se o MSIX nao estiver na pasta, selecione manualmente quando for pedido.
pause

:end
endlocal
exit /b %EXITCODE%