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
:: PASO 1 - Crear escena + prefabs + enlazar + build Windows x64
:: ==================================================================
echo [1/2] Construyendo proyecto (Escena, Prefabs, Link, Build)...
echo       Log en: %LOG_BUILD%
echo.

%UNITY% -quit -batchmode ^
    -projectPath %PROJECT% ^
    -executeMethod Redes.EditorTools.RedesBuildAll.FullBuildCLI ^
    -logFile "%LOG_BUILD%"

set BUILD_EXIT=%ERRORLEVEL%

:: Mostrar ultimas lineas del log de build
echo.
echo --- Ultimas lineas de log_build.txt ---
powershell -command "if (Test-Path '%LOG_BUILD%') { Get-Content '%LOG_BUILD%' | Select-Object -Last 30 } else { Write-Host 'Log no generado' }"
echo ----------------------------------------
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
echo       Log en: %LOG_TESTS%
echo       Resultados en: %RESULTS%
echo.

%UNITY% -batchmode ^
    -projectPath %PROJECT% ^
    -runTests ^
    -testPlatform EditMode ^
    -testResults "%RESULTS%" ^
    -logFile "%LOG_TESTS%"

set TEST_EXIT=%ERRORLEVEL%

:: Parsear resultado XML si existe
echo.
echo --- Resultado de tests ---
powershell -command ^
    "if (Test-Path '%RESULTS%') { " ^
    "  [xml]$x = Get-Content '%RESULTS%'; " ^
    "  $r = $x.'test-run'; " ^
    "  Write-Host ('Total: ' + $r.total + '  Passed: ' + $r.passed + '  Failed: ' + $r.failed + '  Skipped: ' + $r.skipped); " ^
    "  if ([int]$r.failed -gt 0) { " ^
    "    $x.SelectNodes('//test-case[@result=\"Failed\"]') | ForEach-Object { Write-Host ('  FAIL: ' + $_.name) } " ^
    "  } " ^
    "} else { Write-Host 'XML de resultados no encontrado' }"
echo --------------------------
echo.

if %TEST_EXIT% NEQ 0 (
    echo [WARN] Algunos tests fallaron (exit %TEST_EXIT%). Revisa test_results.xml y log_tests.txt
    echo        Esto NO bloquea el build si ya se genero correctamente.
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
) else (
    echo   EXE:    [no generado - revisa log_build.txt]
)
echo ======================================================
echo.
pause
