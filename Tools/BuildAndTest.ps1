param(
    [string]$ProjectRoot = ".",
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe",
    [switch]$RunTests,
    [switch]$RollbackOnFail
)

$ErrorActionPreference = "Stop"
$LogFile = Join-Path $ProjectRoot "unity_compile_scripts.log"

Write-Host "--- BuildAndTest: Iniciando Compilacion ---"
Write-Host "Proyecto: $ProjectRoot"
Write-Host "Unity: $UnityPath"

# Validar Unity
if (-not (Test-Path $UnityPath)) {
    # Intentar buscar en ubicaciones comunes
    $commonPaths = @(
        "C:\Program Files\Unity\Editor\Unity.exe",
        "C:\Program Files (x86)\Unity\Editor\Unity.exe"
    )
    foreach ($p in $commonPaths) {
        if (Test-Path $p) {
            $UnityPath = $p
            break
        }
    }
}

if (-not (Test-Path $UnityPath)) {
    Write-Error "No se encontro el ejecutable de Unity. Por favor especifique la ruta correcta."
    exit 1
}

# Ejecutar compilacion en batchmode
Write-Host "Ejecutando Unity compiler..."
if (Test-Path $LogFile) { Remove-Item $LogFile -Force }

$arguments = "-batchmode -quit -projectPath `"$ProjectRoot`" -logFile `"$LogFile`""
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -NoNewWindow -PassThru

# Analizar log de compilacion buscando errores
$compiledOk = $true
if (Test-Path $LogFile) {
    $logContent = Get-Content $LogFile
    $errors = $logContent | Where-Object { $_ -match "error CS\d+" -or $_ -match "Compilation failed" }
    if ($errors) {
        Write-Host "ERRORES DE COMPILACION DETECTADOS:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        $compiledOk = $false
    } else {
        Write-Host "Compilacion exitosa sin errores C#." -ForegroundColor Green
    }
} else {
    Write-Host "No se genero el archivo de log: $LogFile" -ForegroundColor Yellow
    $compiledOk = $false
}

# Pruebas Unitarias si se solicita
$testsOk = $true
if ($compiledOk -and $RunTests) {
    Write-Host "Ejecutando Unity Test Runner..."
    $testResultsFile = Join-Path $ProjectRoot "TestResults.xml"
    if (Test-Path $testResultsFile) { Remove-Item $testResultsFile -Force }

    $testArgs = "-batchmode -runTests -projectPath `"$ProjectRoot`" -testPlatform PlayMode -testResults `"$testResultsFile`""
    $testProcess = Start-Process -FilePath $UnityPath -ArgumentList $testArgs -Wait -NoNewWindow -PassThru
    
    if (Test-Path $testResultsFile) {
        $resultsXml = [xml](Get-Content $testResultsFile)
        $failedTests = $resultsXml.SelectNodes("//test-case[@result='Failed']")
        if ($failedTests.Count -gt 0) {
            Write-Host "PRUEBAS FALLIDAS: $($failedTests.Count)" -ForegroundColor Red
            foreach ($t in $failedTests) {
                Write-Host " - $($t.name): $($t.failure.message)" -ForegroundColor Red
            }
            $testsOk = $false
        } else {
            Write-Host "Todas las pruebas unitarias pasaron con exito." -ForegroundColor Green
        }
    } else {
        Write-Host "No se encontraron resultados de pruebas." -ForegroundColor Yellow
        $testsOk = $false
    }
}

# Rollback si algo fallo
if ((-not $compiledOk -or -not $testsOk) -and $RollbackOnFail) {
    Write-Host "Iniciando rollback automatico..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    $stashName = "rollback-stash-" + (Get-Date -Format "yyyyMMdd-HHmmss")
    git stash save "$stashName"
    Write-Host "Cambios guardados en git stash: $stashName" -ForegroundColor Yellow
    
    # Crear rama de recuperacion opcional
    $branchName = "recovery/" + $stashName
    git stash branch $branchName
    Write-Host "Creada rama de recuperacion: $branchName con los cambios conflictivos." -ForegroundColor Yellow
}

# Resumen de resultados
Write-Host "--- BuildAndTest: Finalizado ---"
if ($compiledOk -and $testsOk) {
    Write-Host "ESTADO GENERAL: EXITOSO" -ForegroundColor Green
    exit 0
} else {
    Write-Host "ESTADO GENERAL: CON ERRORES" -ForegroundColor Red
    exit 1
}
