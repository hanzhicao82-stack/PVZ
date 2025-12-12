$files = Get-ChildItem -Path "Assets\Scripts" -Recurse -Filter "*.cs"

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $modified = $false
    
    # 替换 namespace 声明
    if ($content -match 'namespace PVZ
        $content = $content -replace 'namespace PVZ
        $content = $content -replace 'namespace PVZ
        $content = $content -replace 'namespace PVZ
        $modified = $true
    }
    
    # 替换 using 声明
    $newContent = $content `
        -replace 'using PVZ\.Common\.Components', 'using Common.Components' `
        -replace 'using PVZ\.Common\.Systems', 'using Common.Systems' `
        -replace 'using PVZ\.Common\.Modules', 'using Common.Modules' `
        -replace 'using PVZ\.SpecGame\.PVE\.Components', 'using PVZ.Components' `
        -replace 'using PVZ\.SpecGame\.PVE\.Systems', 'using PVZ.Systems' `
        -replace 'using PVZ\.SpecGame\.PVE\.Config', 'using PVZ.Config' `
        -replace 'using PVZ\.SpecGame\.PVE\.Data', 'using PVZ.Data' `
        -replace 'using PVZ\.Type\.TowerDefense\.Components', 'using PVZ.Components' `
        -replace 'using PVZ\.Type\.TowerDefense\.Systems', 'using PVZ.Systems' `
        -replace 'using PVZ\.DOTS\.Utils', 'using Framework.Utils' `
        -replace 'using PVZ\.DOTS\.Tools', 'using PVZ.Tools' `
        -replace 'using PVZ\.Framework\.', 'using Framework.' `
        -replace 'using PVZ\.DOTS\.Authoring', 'using PVZ.Authoring'
    
    if ($newContent -ne $content) {
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "Fixed: $($file.Name)"
    }
}

Write-Host "`nDone!"
