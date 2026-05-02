# Delete all auto-generated Designer.cs files to force regeneration
$projectPath = "C:\Users\john_\Source\Repos\SFXPlayer_fork\SFXPlayer\SFXPlayer"
$designerFiles = Get-ChildItem -Path $projectPath -Filter "*.Designer.cs" -Recurse

Write-Host "Deleting $($designerFiles.Count) Designer.cs files..." -ForegroundColor Yellow

foreach ($file in $designerFiles) {
    if ($file.Name -ne "Resources.Designer.cs") {  # Keep Resources.Designer.cs
        Write-Host "  Deleting: $($file.Name)" -ForegroundColor Gray
        Remove-Item $file.FullName -Force
    }
}

Write-Host "Done! Designer files will be regenerated on next build." -ForegroundColor Green