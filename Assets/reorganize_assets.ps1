# PowerShell script to fully reorganize Unity Assets folder

$projectPath = "C:\Users\PC_JOACO\Documents\DV_C5_Juego\Assets"

# Define top level clean structure folders
$folders = @(
    "Plugins",
    "Textures",
    "Materials",
    "Prefabs",
    "Scenes",
    "Scenes/Gameplay",
    "Scenes/Menus",
    "Scenes/Tests",
    "Audio",
    "Scripts",
    "Scripts/Gameplay",
    "Scripts/VFX",
    "Shaders",
    "UI",
    "UI/Images",
    "Animations",
    "Otros"
)

# Helper function to ensure folders exist
foreach ($f in $folders) {
    $full = Join-Path $projectPath $f
    if (-not (Test-Path $full)) {
        New-Item -ItemType Directory -Path $full | Out-Null
    }
}

# Helper to move an item safely along with its .meta file
function Move-AssetWithMeta($item, $targetDir) {
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir | Out-Null
    }
    $destFile = Join-Path $targetDir $item.Name
    if (Test-Path $item.FullName) {
        Move-Item -LiteralPath $item.FullName -Destination $destFile -Force
        $metaPath = $item.FullName + ".meta"
        if (Test-Path $metaPath) {
            $destMeta = $destFile + ".meta"
            Move-Item -LiteralPath $metaPath -Destination $destMeta -Force
        }
    }
}

# 1. Reorganize Plugins: move library folders and external assets to Plugins folder
# Custom Inspector, TextMesh Pro, ParrelSync, VFXPACK_IMPACT_WALLCOEUR_FreeVersion (and any leftover examenes folders)
$libs = @("Custom Inspector", "ParrelSync", "TextMesh Pro", "VFXPACK_IMPACT_WALLCOEUR_FreeVersion")
foreach ($libName in $libs) {
    $libFolder = Join-Path $projectPath $libName
    if (Test-Path $libFolder) {
        $dest = Join-Path $projectPath ("Plugins/" + $libName)
        if (-not (Test-Path $dest)) {
            New-Item -ItemType Directory -Path $dest | Out-Null
        }
        Get-ChildItem -Path $libFolder -File | ForEach-Object { Move-AssetWithMeta $_ $dest }
        Get-ChildItem -Path $libFolder -Directory | ForEach-Object {
            $subDest = Join-Path $dest $_.Name
            Move-Item -LiteralPath $_.FullName -Destination $subDest -Force
            $m = $_.FullName + ".meta"
            if (Test-Path $m) {
                Move-Item -LiteralPath $m -Destination ($subDest + ".meta") -Force
            }
        }
        # Move the folder meta file itself
        $folderMeta = $libFolder + ".meta"
        if (Test-Path $folderMeta) {
            Move-Item -LiteralPath $folderMeta -Destination ($dest + ".meta") -Force
        }
        Remove-Item -LiteralPath $libFolder -Force -Recurse -ErrorAction SilentlyContinue
        # If any meta remains, remove it
        if (Test-Path $folderMeta) {
            Remove-Item -LiteralPath $folderMeta -Force
        }
    }
}

# 2. Reorganize and move scene files into categorised subfolders:
# - Menu* -> Scenes/Menus
# - Game*, Nivel* -> Scenes/Gameplay
# - test*, Test* -> Scenes/Tests
Get-ChildItem -Path $projectPath -Recurse -Filter *.unity | ForEach-Object {
    if ($_.FullName -notlike "*\Photon\*" -and $_.FullName -notlike "*\Plugins\*") {
        if ($_.Name -like "Menu*") {
            Move-AssetWithMeta $_ (Join-Path $projectPath "Scenes/Menus")
        } elseif ($_.Name -like "Game*" -or $_.Name -like "Nivel*") {
            Move-AssetWithMeta $_ (Join-Path $projectPath "Scenes/Gameplay")
        } else {
            Move-AssetWithMeta $_ (Join-Path $projectPath "Scenes/Tests")
        }
    }
}

# 3. Categorize Scripts
# - Scripts from external libraries should reside in Plugins
# - Core game script files go to Scripts/Gameplay
# - VFX related scripts go to Scripts/VFX
Get-ChildItem -Path $projectPath -Recurse -Filter *.cs | ForEach-Object {
    if ($_.FullName -notlike "*\Photon\*" -and $_.FullName -notlike "*\Plugins\*") {
        if ($_.Name -match "Vfx" -or $_.Name -match "VFX" -or $_.Name -match "Effect") {
            Move-AssetWithMeta $_ (Join-Path $projectPath "Scripts/VFX")
        } else {
            Move-AssetWithMeta $_ (Join-Path $projectPath "Scripts/Gameplay")
        }
    }
}

# 4. Clean root asset files / organize other asset files
# Move animations (.anim, .controller)
Get-ChildItem -Path $projectPath -Recurse -Include *.anim, *.controller | ForEach-Object {
    if ($_.FullName -notlike "*\Photon\*" -and $_.FullName -notlike "*\Plugins\*") {
        Move-AssetWithMeta $_ (Join-Path $projectPath "Animations")
    }
}

# Move Shaders
Get-ChildItem -Path $projectPath -Recurse -Include *.shader, *.cginc | ForEach-Object {
    if ($_.FullName -notlike "*\Photon\*" -and $_.FullName -notlike "*\Plugins\*") {
        Move-AssetWithMeta $_ (Join-Path $projectPath "Shaders")
    }
}

# Move Audio
Get-ChildItem -Path $projectPath -Recurse -Include *.mp3, *.wav, *.ogg | ForEach-Object {
    if ($_.FullName -notlike "*\Photon\*" -and $_.FullName -notlike "*\Plugins\*") {
        Move-AssetWithMeta $_ (Join-Path $projectPath "Audio")
    }
}

# Move Materials & Textures
Get-ChildItem -Path $projectPath -Recurse -Filter *.mat | ForEach-Object {
    if ($_.FullName -notlike "*\Photon\*" -and $_.FullName -notlike "*\Plugins\*") {
        Move-AssetWithMeta $_ (Join-Path $projectPath "Materials")
    }
}

Get-ChildItem -Path $projectPath -Recurse -Include *.png, *.jpg, *.tga, *.psd | ForEach-Object {
    if ($_.FullName -notlike "*\Photon\*" -and $_.FullName -notlike "*\Plugins\*") {
        # Split UI textures vs normal textures
        if ($_.FullName -like "*\UI\*" -or $_.Name -match "Boton" -or $_.Name -match "Mouse" -or $_.Name -match "Victoria" -or $_.Name -match "Inicio" -or $_.Name -match "botiquin" -or $_.Name -match "jeringa") {
            Move-AssetWithMeta $_ (Join-Path $projectPath "UI/Images")
        } else {
            Move-AssetWithMeta $_ (Join-Path $projectPath "Textures")
        }
    }
}

# Move Prefabs
Get-ChildItem -Path $projectPath -Recurse -Filter *.prefab | ForEach-Object {
    if ($_.FullName -notlike "*\Photon\*" -and $_.FullName -notlike "*\Plugins\*") {
        Move-AssetWithMeta $_ (Join-Path $projectPath "Prefabs")
    }
}

# 5. Handle VFX folder name rules: Apply '_vfx' suffix to all visual effects assets
# (e.g. assets in Plugins/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/03_Texture that were renamed or custom textures)
# Let's locate user-defined VFX textures and apply naming convention
Get-ChildItem -Path $projectPath -Recurse -Include *.png | ForEach-Object {
    if ($_.FullName -like "*\VFX*" -and $_.FullName -notlike "*\Photon\*") {
        $base = $_.BaseName
        if (-not $base.EndsWith("_vfx")) {
            $newName = $base + "_vfx" + $_.Extension
            $destPath = Join-Path $_.DirectoryName $newName
            Rename-Item -LiteralPath $_.FullName -NewName $newName -Force
            $meta = $_.FullName + ".meta"
            if (Test-Path $meta) {
                Rename-Item -LiteralPath $meta -NewName ($newName + ".meta") -Force
            }
        }
    }
}

# 6. Put other miscellaneous assets/folders in "Otros"
$reserved = @("Plugins", "Textures", "Materials", "Prefabs", "Scenes", "Audio", "Scripts", "Shaders", "UI", "Animations", "Otros", "Photon")
Get-ChildItem -Path $projectPath -File | ForEach-Object {
    if ($reserved -notcontains $_.Name -and $_.Name -notlike "*.meta" -and $_.Name -notmatch "reorganize_assets") {
        Move-AssetWithMeta $_ (Join-Path $projectPath "Otros")
    }
}

Get-ChildItem -Path $projectPath -Directory | ForEach-Object {
    if ($reserved -notcontains $_.Name) {
        $dest = Join-Path $projectPath ("Otros/" + $_.Name)
        Move-Item -LiteralPath $_.FullName -Destination $dest -Force
        $m = $_.FullName + ".meta"
        if (Test-Path $m) {
            Move-Item -LiteralPath $m -Destination ($dest + ".meta") -Force
        }
    }
}

# Clean any empty directories (except reserved top ones)
function Remove-EmptyDirectories($path) {
    Get-ChildItem -Path $path -Directory | ForEach-Object {
        Remove-EmptyDirectories $_.FullName
    }
    $contents = Get-ChildItem -Path $path
    $isReserved = $reserved -contains (Split-Path $path -Leaf)
    if ($contents.Count -eq 0 -and -not $isReserved -and $path -ne $projectPath) {
        Remove-Item -LiteralPath $path -Force -Recurse -ErrorAction SilentlyContinue
        $meta = $path + ".meta"
        if (Test-Path $meta) {
            Remove-Item -LiteralPath $meta -Force -ErrorAction SilentlyContinue
        }
    }
}
Remove-EmptyDirectories $projectPath

Write-Host "Reorganization complete!"
