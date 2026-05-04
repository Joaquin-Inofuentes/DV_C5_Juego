@echo off
setlocal enabledelayedexpansion

set "OUTPUT=Concatenado.txt"
set "COUNT=0"

if exist "%OUTPUT%" del "%OUTPUT%"

echo ==== INICIO CONCATENACION ==== > "%OUTPUT%"
echo Carpeta base: %CD% >> "%OUTPUT%"
echo. >> "%OUTPUT%"

for /r %%f in (*.cs) do (
    echo.
    echo Procesando: %%f

    echo ============================== >> "%OUTPUT%"
    echo ARCHIVO: %%f >> "%OUTPUT%"
    echo ============================== >> "%OUTPUT%"
    type "%%f" >> "%OUTPUT%"
    echo. >> "%OUTPUT%"

    set /a COUNT+=1
)

echo.
if %COUNT% GTR 0 (
    echo Logrado. Archivos concatenados: %COUNT%
) else (
    echo Fallido. No se encontraron archivos .cs
)

echo.
echo Lista de archivos encontrados:
for /r %%f in (*.cs) do echo %%f

echo.
pause