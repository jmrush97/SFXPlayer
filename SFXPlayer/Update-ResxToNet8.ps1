# PowerShell script to update all .resx files from .NET Framework to .NET 8
# Run this from the SFXPlayer project root directory

Write-Host "Updating .resx files to .NET 8..." -ForegroundColor Cyan

$projectRoot = "C:\Users\john_\Source\Repos\SFXPlayer_fork\SFXPlayer"
$resxFiles = @(
    "$projectRoot\SFXPlayer\Properties\Resources.resx",
    "$projectRoot\SFXPlayer\forms\MSCEventEditor.resx",
    "$projectRoot\SFXPlayer\forms\PlayStrip.resx",
    "$projectRoot\SFXPlayer\forms\SFXPlayer.resx",
    "$projectRoot\SFXPlayer\forms\Spacer.resx",
    "$projectRoot\SFXPlayer\forms\SplashScreen.resx",
    "$projectRoot\SFXPlayer\forms\TimeStamper.resx",
    "$projectRoot\SFXPlayer\forms\ucVolume.resx",
    "$projectRoot\SFXPlayer\ResourcesSvg.resx"
)

$updatedCount = 0
$errorCount = 0

foreach ($file in $resxFiles) {
    if (Test-Path $file) {
        try {
            Write-Host "Processing: $file" -ForegroundColor Yellow
            $content = Get-Content $file -Raw -Encoding UTF8

            # Update ResXResourceReader version
            $content = $content -replace 'System\.Resources\.ResXResourceReader,\s*System\.Windows\.Forms,\s*Version=[\d\.]+', 'System.Resources.ResXResourceReader, System.Windows.Forms, Version=8.0.0.0'

            # Update ResXResourceWriter version
            $content = $content -replace 'System\.Resources\.ResXResourceWriter,\s*System\.Windows\.Forms,\s*Version=[\d\.]+', 'System.Resources.ResXResourceWriter, System.Windows.Forms, Version=8.0.0.0'

            # Update System.Windows.Forms assembly references
            $content = $content -replace '(name="System\.Windows\.Forms"[^>]*>System\.Windows\.Forms),\s*Version=[\d\.]+', '$1, Version=8.0.0.0'
            $content = $content -replace '(System\.Windows\.Forms),\s*Version=[\d\.]+', '$1, Version=8.0.0.0'

            # Update System.Drawing references
            $content = $content -replace '(System\.Drawing),\s*Version=[\d\.]+', '$1, Version=8.0.0.0'

            # Update version header from 1.3 to 2.0 if needed
            $content = $content -replace '<resheader name="version">\s*<value>1\.3</value>', '<resheader name="version">`n    <value>2.0</value>'

            # Save the file with UTF-8 encoding
            Set-Content $file $content -NoNewline -Encoding UTF8
            Write-Host "  ✓ Successfully updated" -ForegroundColor Green
            $updatedCount++
        }
        catch {
            Write-Host "  ✗ Error updating file: $_" -ForegroundColor Red
            $errorCount++
        }
    } else {
        Write-Host "  ⚠ File not found: $file" -ForegroundColor Magenta
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Updated: $updatedCount files" -ForegroundColor Green
Write-Host "  Errors: $errorCount files" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })
Write-Host "========================================`n" -ForegroundColor Cyan

if ($updatedCount -gt 0) {
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Open Visual Studio 2022" -ForegroundColor White
    Write-Host "2. Clean the solution (Build > Clean Solution)" -ForegroundColor White
    Write-Host "3. Rebuild the solution (Build > Rebuild Solution)" -ForegroundColor White
    Write-Host "4. If Resources class is still internal, manually change it to public in Resources.Designer.cs" -ForegroundColor White
}