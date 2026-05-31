param(
    [string]$ProjectRoot = ".",
    [switch]$Commit
)

$ErrorActionPreference = "Stop"
$AssetsPath = Join-Path $ProjectRoot "Assets"
$DocsPath   = Join-Path $AssetsPath "Docs"
$FileIndexPath = Join-Path $DocsPath "FileIndex"

if (-not (Test-Path $AssetsPath)) {
    Write-Error "No se encontro Assets en: $AssetsPath"
    exit 1
}

if (-not (Test-Path $DocsPath)) { New-Item -ItemType Directory -Path $DocsPath -Force | Out-Null }
if (-not (Test-Path $FileIndexPath)) { New-Item -ItemType Directory -Path $FileIndexPath -Force | Out-Null }

Write-Host "--- RefreshDocs: Iniciando ---"

$scripts = @(Get-ChildItem -Path $AssetsPath -Recurse -Include *.cs -File |
           Where-Object { $_.FullName -notmatch '\\Plugins\\|\\TextMesh Pro\\|\\ParrelSync\\|\\Custom Inspector\\|\\Photon\\|\\VFXPACK' })

$scenes  = @(Get-ChildItem -Path $AssetsPath -Recurse -Include *.unity -File)
$prefabs = @(Get-ChildItem -Path $AssetsPath -Recurse -Include *.prefab -File |
           Where-Object { $_.FullName -notmatch '\\Plugins\\' })

Write-Host "Scripts propios: $($scripts.Count)"
Write-Host "Escenas: $($scenes.Count)"
Write-Host "Prefabs: $($prefabs.Count)"

$inventoryFile = Join-Path $DocsPath "ProjectInventory.md"
$inventoryContent = @"
# Inventario del Proyecto

- Ultima actualizacion: $(Get-Date -Format 'yyyy-MM-dd HH:mm')
- Total de scripts C# (propios): $($scripts.Count)
- Escenas: $($scenes.Count)
- Prefabs: $($prefabs.Count)

## Lista de Scripts
"@

$i = 1
foreach ($s in ($scripts | Sort-Object FullName)) {
    $relPath = $s.FullName.Replace($ProjectRoot, "").TrimStart("\")
    $inventoryContent += "`n$i. **$($s.Name)** (`$relPath`)"
    $i++
}

Set-Content -Path $inventoryFile -Value $inventoryContent -Encoding UTF8

$updatedCount = 0
foreach ($script in $scripts) {
    $mdName = $script.BaseName + ".md"
    $mdPath = Join-Path $FileIndexPath $mdName
    $relPath = $script.FullName.Replace($ProjectRoot, "").TrimStart("\")
    
    $content = Get-Content $script.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    $clases = [regex]::Matches($content, 'class\s+(\w+)') | ForEach-Object { $_.Groups[1].Value }
    $clasesStr = if ($clases) { ($clases -join ", ") } else { "No detectada" }

    $metodos = [regex]::Matches($content, 'public\s+(?:static\s+)?(?:override\s+)?(?:virtual\s+)?[\w<>\[\]]+\s+(\w+)\s*\(') |
               ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
    $metodosStr = if ($metodos) { ($metodos | ForEach-Object { "- $_()" }) -join "`n" } else { "- Ninguno detectado" }

    $usings = [regex]::Matches($content, 'using\s+([\w\.]+);') | ForEach-Object { $_.Groups[1].Value }
    $usingsStr = if ($usings) { ($usings | ForEach-Object { "- $_" }) -join "`n" } else { "- Ninguno" }

    $eventos = [regex]::Matches($content, '(?:event\s+|Action|UnityEvent)\s*<?[\w,\s]*>?\s+(\w+)') |
               ForEach-Object { $_.Groups[1].Value }
    $eventosStr = if ($eventos) { ($eventos | ForEach-Object { "- $_" }) -join "`n" } else { "- Ninguno detectado" }

    $lineCount = ($content -split "`n").Count

    $fileContent = @"
# $($script.BaseName)

- Archivo: `$relPath`
- Lineas: $lineCount
- Clase(s): $clasesStr

## Metodos Publicos Clave
$metodosStr

## Eventos
$eventosStr

## Dependencias (using)
$usingsStr
"@

    Set-Content -Path $mdPath -Value $fileContent -Encoding UTF8
    $updatedCount++
}

Write-Host "FileIndex actualizado: $updatedCount archivos."
Write-Host "--- RefreshDocs: Finalizado ---"
