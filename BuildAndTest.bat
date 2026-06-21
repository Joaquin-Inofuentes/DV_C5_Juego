@echo off
setlocal enabledelayedexpansion

echo.
echo ======================================================
echo   REDES - Build + Test completo
echo ======================================================
echo.

:: ---- Rutas ----
set UNITY="C:\Program Files\Unity\Hub\Editor\2022.3.5f1\Editor\Unity.exe"
set PROJECT="%~dp0"
set LOG_BUILD=%~dp0log_build.txt
set LOG_TESTS=%~dp0log_tests.txt
set RESULTS=%~dp0test_results.xml

:: ---- Verificar Unity ----
if not exist %UNITY% (
    echo [ERROR] Unity no encontrado en %UNITY%
    echo        Ajusta la ruta UNITY al inicio del bat.
    pause
    exit /b 1
)

:: ==================================================================
:: JERARQUIA - Imprimir antes del build para diagnostico
:: ==================================================================
echo === JERARQUIA RedesGame.unity (referencias) ===
echo.
echo  Main Camera         [Camera ortho pos=(0,12,0) rot=Euler(90,0,0)]
echo  Directional Light   [Light Directional]
echo  Ground              [Plane scale=(2,1,2)]
echo  EventSystem         [EventSystem, StandaloneInputModule]
echo  NetworkRunner       [NetworkRunner, HostNetworkService, PlayerSpawner]
echo    _playerSpawner  --^> PlayerSpawner (mismo GO)
echo    _playerPrefab   --^> Assets/_Redes/Prefabs/Player.prefab
echo  GameManager         [NetworkObject, MatchNetworkController]
echo  Controllers         [GameFlowController, MatchController, PlayerController]
echo    GameFlowController:
echo      _hostService  --^> HostNetworkService     [OK]
echo      _lobbyView    --^> LobbyView MonoBeh.     [OK]
echo      _gameHudView  --^> GameHudView MonoBeh.   [OK]
echo      _matchController --^> MatchController     [OK]
echo  Canvas (ScreenSpaceOverlay)
echo    LobbyView (stretch)
echo      _statusText      --^> StatusText.Text     [OK]
echo      _playerCountText --^> PlayerCountText.Text[OK]
echo      _hostButton      --^> HostButton.Button   [OK]
echo      _joinButton      --^> JoinButton.Button   [OK]
echo      StatusText       "Crear sala o unirse?"
echo      PlayerCountText  "Jugadores: 0/2"
echo      HostButton       (-130,20)  onClick=CreateRoom
echo      JoinButton       (130,20)   onClick=JoinRoom
echo    GameHudView (stretch)
echo      HealthText / AmmoText
echo    ResultView (stretch)
echo      ResultPanel [INACTIVE] - ResultText
echo.
echo *** CAUSA DEL BUG ANTERIOR: Build usaba Scene_Game como escena 0 ***
echo *** SOLUCION:               Build dedicado solo con RedesGame.unity ***
echo.

:: ==================================================================
:: PASO 1 - Crear escena + prefabs + enlazar + build Windows x64
:: ==================================================================
echo [1/2] Construyendo proyecto...
echo       Log en: %LOG_BUILD%
echo.

%UNITY% -quit -batchmode ^
    -projectPath %PROJECT% ^
    -executeMethod Redes.EditorTools.RedesBuildAll.FullBuildCLI ^
    -logFile "%LOG_BUILD%"

set BUILD_EXIT=%ERRORLEVEL%

echo.
echo --- Ultimas 35 lineas de log_build.txt ---
powershell -command "if (Test-Path '%LOG_BUILD%') { Get-Content '%LOG_BUILD%' | Select-Object -Last 35 } else { Write-Host 'Log no generado' }"
echo -------------------------------------------
echo.

if %BUILD_EXIT% NEQ 0 (
    echo [ERROR] El build fallo (exit %BUILD_EXIT%). Revisa log_build.txt
    pause
    exit /b %BUILD_EXIT%
)
echo [OK] Build completado.
echo.

:: ==================================================================
:: PASO 2 - Tests EditMode
:: ==================================================================
echo [2/2] Ejecutando tests (EditMode)...
echo       Log: %LOG_TESTS%
echo.

%UNITY% -batchmode ^
    -projectPath %PROJECT% ^
    -runTests ^
    -testPlatform EditMode ^
    -testResults "%RESULTS%" ^
    -logFile "%LOG_TESTS%"

set TEST_EXIT=%ERRORLEVEL%

echo.
echo --- Resultado de tests ---
powershell -command ^
    "if (Test-Path '%RESULTS%') { " ^
    "  [xml]$x = Get-Content '%RESULTS%'; " ^
    "  $r = $x.'test-run'; " ^
    "  Write-Host ('Total: ' + $r.total + '  Passed: ' + $r.passed + '  Failed: ' + $r.failed + '  Skipped: ' + $r.skipped); " ^
    "  if ([int]$r.failed -gt 0) { " ^
    "    $x.SelectNodes('//test-case[@result=""Failed""]') | ForEach-Object { Write-Host ('  FAIL: ' + $_.name) } " ^
    "  } " ^
    "} else { Write-Host 'XML de resultados no encontrado' }"
echo --------------------------
echo.

if %TEST_EXIT% NEQ 0 (
    echo [WARN] Algunos tests fallaron. Revisa test_results.xml
) else (
    echo [OK] Todos los tests pasaron.
)

echo.
echo ======================================================
echo   RESULTADO FINAL
echo ======================================================
echo   Build:  log_build.txt
echo   Tests:  log_tests.txt  /  test_results.xml
if exist "%~dp0Builds\RedesGame_Win64\RedesGame.exe" (
    echo   EXE:    Builds\RedesGame_Win64\RedesGame.exe
    echo.
    echo   Para jugar: ejecuta Builds\RedesGame_Win64\RedesGame.exe
    echo   - Jugador 1: inicia el .exe, click CREAR SALA
    echo   - Jugador 2: inicia otro .exe, click UNIRSE A SALA
) else (
    echo   EXE:    [no generado - revisa log_build.txt]
)
echo ======================================================
echo.
pause
