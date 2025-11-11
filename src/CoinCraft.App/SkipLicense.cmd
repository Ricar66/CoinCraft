@echo off
setlocal
set COINCRAFT_SKIP_LICENSE=1
set APP="%~dp0CoinCraft.App.exe"
if exist %APP% (
  start "" %APP%
) else (
  echo Executavel nao encontrado em %~dp0
  pause
)
endlocal