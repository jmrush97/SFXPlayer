# PowerShell script to update all .resx files to .NET 8
$resxFiles = @(
    "SFXPlayer\Properties\Resources.resx",
    "SFXPlayer\forms\MSCEventEditor.resx",
    "SFXPlayer\forms\PlayStrip.resx",
    "SFXPlayer\forms\SFXPlayer.resx",
    "SFXPlayer\forms\Spacer.resx",
    "SFXPlayer\forms\SplashScreen.resx",
    "SFXPlayer\forms\TimeStamper.resx",
    "SFXPlayer\forms\ucVolume.resx",
    "SFXPlayer\ResourcesSvg.resx"
)

foreach ($file in $resxFiles) {
    if (Test-Path $file) {
        Write-Host "Updating $file..."
        $content = Get-Content $file -Raw
        
        # Update reader version
        $content = $content -replace 'System\.Resources\.ResXResourceReader, System\.Windows\.Forms, Version=\d+\.\d+\.\d+\.\d+', 'System.Resources.ResXResourceReader, System.Windows.Forms, Version=8.0.0.0'
        
        # Update writer version
        $content = $content -replace 'System\.Resources\.ResXResourceWriter, System\.Windows\.Forms, Version=\d+\.\d+\.\d+\.\d+', 'System.Resources.ResXResourceWriter, System.Windows.Forms, Version=8.0.0.0'
        
        # Update System.Windows.Forms assembly references
        $content = $content -replace 'System\.Windows\.Forms, Version=\d+\.\d+\.\d+\.\d+', 'System.Windows.Forms, Version=8.0.0.0'
        
        # Update System.Drawing references
        $content = $content -replace 'System\.Drawing, Version=\d+\.\d+\.\d+\.\d+', 'System.Drawing, Version=8.0.0.0'
        
        # Update version in resheader if it's 1.3 to 2.0
        $content = $content -replace '<resheader name="version">\s*<value>1\.3</value>', '<resheader name="version">`n    <value>2.0</value>'
        
        Set-Content $file $content -NoNewline
        Write-Host "  ✓ Updated" -ForegroundColor Green
    } else {
        Write-Host "  ✗ File not found: $file" -ForegroundColor Red
    }
}

Write-Host "`nAll .resx files updated! Now regenerating designer files..." -ForegroundColor Yellow
Write-Host "Please rebuild the solution in Visual Studio to regenerate the Designer.cs files." -ForegroundColor Cyan