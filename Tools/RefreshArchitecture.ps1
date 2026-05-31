param(
    [string]$ProjectRoot = "."
)

$ErrorActionPreference = "Stop"
$AssetsPath = Join-Path $ProjectRoot "Assets"
$DocsPath = Join-Path $AssetsPath "Docs"
$ArchitectureFile = Join-Path $DocsPath "Architecture.md"

if (-not (Test-Path $DocsPath)) {
    New-Item -ItemType Directory -Path $DocsPath -Force | Out-Null
}

Write-Host "--- RefreshArchitecture: Iniciando ---"

# Definir los sistemas y los patrones de ruta para agruparlos
$Systems = [ordered]@{
    "MVC_Base"         = "Scripts/MVC/(?!Squad|Enemy)"
    "MVC_Squad"        = "Scripts/MVC/Squad"
    "MVC_Enemy"        = "Scripts/MVC/Enemy"
    "CambioDeSoldado"  = "Scripts/CambioDeSoldado"
    "SC_USP_Core"      = "Scripts/SC_USP/Core"
    "SC_USP_Entities"  = "Scripts/SC_USP/Entities"
    "SC_USP_IA"        = "Scripts/SC_USP/IA"
    "SC_USP_Weapons"   = "Scripts/SC_USP/Weapons"
    "SC_USP_Services"  = "Scripts/SC_USP/Services"
    "SC_USP_UI"        = "Scripts/SC_USP/UI"
    "Scenes_Base"      = "Scenes/Base"
    "Root_Scripts"     = "Scripts/(?![MVC|SC_USP|CambioDeSoldado|Nuevos])"
}

# Obtener todos los scripts y asignarlos a un sistema
$AssetsPath = Join-Path $ProjectRoot "Assets"
$scripts = Get-ChildItem -Path $AssetsPath -Recurse -Include *.cs -File |
           Where-Object { $_.FullName -notmatch '\\Plugins\\|\\TextMesh Pro\\|\\ParrelSync\\|\\Custom Inspector\\|\\Photon\\|\\VFXPACK' }

$ScriptToSystem = @{}
$SystemScripts = @{}
foreach ($sys in $Systems.Keys) { $SystemScripts[$sys] = @() }

foreach ($s in $scripts) {
    $relPath = $s.FullName.Replace($ProjectRoot, "").Replace("\", "/")
    
    $assigned = $false
    foreach ($sys in $Systems.Keys) {
        $pattern = $Systems[$sys]
        if ($relPath -match $pattern) {
            $ScriptToSystem[$s.BaseName] = $sys
            $SystemScripts[$sys] += $s.BaseName
            $assigned = $true
            break
        }
    }
    if (-not $assigned) {
        # Si no encaja en ninguno, lo metemos en Root_Scripts
        $ScriptToSystem[$s.BaseName] = "Root_Scripts"
        $SystemScripts["Root_Scripts"] += $s.BaseName
    }
}

# Analizar las referencias entre scripts para inferir dependencias entre sistemas
$Dependencies = @{}
foreach ($sys in $Systems.Keys) { $Dependencies[$sys] = @{} }

foreach ($s in $scripts) {
    $currentSystem = $ScriptToSystem[$s.BaseName]
    if (-not $currentSystem) { continue }
    
    $content = Get-Content $s.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    # Buscar menciones a otras clases en el código
    foreach ($otherScript in $scripts) {
        if ($otherScript.BaseName -eq $s.BaseName) { continue }
        
        $otherSystem = $ScriptToSystem[$otherScript.BaseName]
        if (-not $otherSystem -or $otherSystem -eq $currentSystem) { continue }

        # Regex simple para buscar el nombre de la otra clase completo (boundary)
        if ($content -match "\b$($otherScript.BaseName)\b") {
            $Dependencies[$currentSystem][$otherSystem] = $true
        }
    }
}

# Generar archivo Docs/Architecture.md
$archContent = @"
# Mapa de Arquitectura y Dependencias de Sistemas

Este documento describe la relacion y dependencias entre los diferentes dominios o sistemas del proyecto.

## Diagrama de Relaciones (Mermaid)

```mermaid
graph TD
"@

# Agregar nodos
foreach ($sys in $Systems.Keys) {
    $count = $SystemScripts[$sys].Count
    $archContent += "    " + $sys + "[" + $sys + " (" + $count + " scripts)]`n"
}

# Agregar conexiones
$connections = @()
foreach ($src in $Dependencies.Keys) {
    foreach ($dst in $Dependencies[$src].Keys) {
        $connections += "    $src --> $dst"
    }
}

$archContent += ($connections | Sort-Object -Unique) -join "`n"
$archContent += "`n" + '```'

$archContent += @"


## Resumen de Sistemas

"@

foreach ($sys in $Systems.Keys) {
    $archContent += "`n### " + $sys + "`n"
    $archContent += "- **Ruta de busqueda**: " + '``' + $Systems[$sys] + '``' + "`n"
    $archContent += "- **Cantidad de scripts**: " + $SystemScripts[$sys].Count + "`n"
    $archContent += "- **Scripts**:`n"
    foreach ($scr in ($SystemScripts[$sys] | Sort-Object)) {
        $archContent += "  - [" + $scr + "](file:///Assets/Docs/FileIndex/" + $scr + ".md)`n"
    }
}

Set-Content -Path $ArchitectureFile -Value $archContent -Encoding UTF8
Write-Host "Architecture.md actualizado con exito."
Write-Host "--- RefreshArchitecture: Finalizado ---"
