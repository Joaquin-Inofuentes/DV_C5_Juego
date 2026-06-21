@echo off
echo ===================================================
echo Iniciando Correcion y Tests de Unity
echo ===================================================

:: Configurar rutas
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.5f1\Editor\Unity.exe"
set PROJECT_PATH="%~dp0"

:: 1. Correr el metodo de correccion de escenas y prefabs
echo [1/2] Ejecutando correccion de escena activa...
%UNITY_PATH% -quit -batchmode -projectPath %PROJECT_PATH% -executeMethod DebugSystem.Editor.CreateScenesAndPrefabsMenu.FixAndConfigureScene -logFile log_corregir.txt

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Hubo un problema al corregir la escena. Revisa log_corregir.txt
    pause
    exit /b %ERRORLEVEL%
)
echo [OK] Escena corregida con exito.

:: 2. Ejecutar los tests automaticos (EditMode)
echo [2/2] Ejecutando tests unitarios...
%UNITY_PATH% -batchmode -projectPath %PROJECT_PATH% -runTests -testPlatform EditMode -testResults test_results.xml -logFile log_tests.txt

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Algunos tests fallaron o hubo errores de compilacion. Revisa log_tests.txt y test_results.xml
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================
echo [PROCESO COMPLETADO] Escena corregida y todos los tests pasaron.
echo ===================================================
pause
