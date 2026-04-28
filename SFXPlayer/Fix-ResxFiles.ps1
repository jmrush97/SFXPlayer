# Fix all .resx files for .NET 8
$ErrorActionPreference = "Stop"

$projectPath = "C:\Users\john_\Source\Repos\SFXPlayer_fork\SFXPlayer\SFXPlayer"
$resxFiles = Get-ChildItem -Path $projectPath -Filter "*.resx" -Recurse

Write-Host "Found $($resxFiles.Count) .resx files to update" -ForegroundColor Cyan

foreach ($file in $resxFiles) {
    Write-Host "`nProcessing: $($file.FullName)" -ForegroundColor Yellow

    $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    $updated = $false

    # Update ResXResourceReader
    if ($content -match 'System\.Resources\.ResXResourceReader.*?Version=\d+\.\d+\.\d+\.\d+') {
        $content = $content -replace '(System\.Resources\.ResXResourceReader,\s*System\.Windows\.Forms,\s*)Version=\d+\.\d+\.\d+\.\d+', '${1}Version=8.0.0.0'
        $updated = $true
    }

    # Update ResXResourceWriter
    if ($content -match 'System\.Resources\.ResXResourceWriter.*?Version=\d+\.\d+\.\d+\.\d+') {
        $content = $content -replace '(System\.Resources\.ResXResourceWriter,\s*System\.Windows\.Forms,\s*)Version=\d+\.\d+\.\d+\.\d+', '${1}Version=8.0.0.0'
        $updated = $true
    }

    # Update assembly alias
    if ($content -match 'name="System\.Windows\.Forms".*?Version=\d+\.\d+\.\d+\.\d+') {
        $content = $content -replace '(name="System\.Windows\.Forms"[^>]*>System\.Windows\.Forms,\s*)Version=\d+\.\d+\.\d+\.\d+', '${1}Version=8.0.0.0'
        $updated = $true
    }

    # Update System.Drawing references
    if ($content -match 'System\.Drawing,\s*Version=\d+\.\d+\.\d+\.\d+') {
        $content = $content -replace '(System\.Drawing,\s*)Version=\d+\.\d+\.\d+\.\d+', '${1}Version=8.0.0.0'
        $updated = $true
    }

    # Update version 1.3 to 2.0
    if ($content -match '<value>1\.3</value>') {
        $content = $content -replace '<value>1\.3</value>', '<value>2.0</value>'
        $updated = $true
    }

    if ($updated) {
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
        Write-Host "  ✓ Updated" -ForegroundColor Green
    } else {
        Write-Host "  → No changes needed" -ForegroundColor Gray
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "All .resx files updated!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan