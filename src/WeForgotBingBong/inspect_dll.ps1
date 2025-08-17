# PowerShell script to inspect Assembly-CSharp.dll
Write-Host "Starting DLL inspection..." -ForegroundColor Green

try {
    $assembly = [System.Reflection.Assembly]::LoadFile("$PWD\lib\Assembly-CSharp.dll")
    Write-Host "Successfully loaded DLL: $($assembly.FullName)" -ForegroundColor Green
    Write-Host "Total types: $($assembly.GetTypes().Count)" -ForegroundColor Yellow
    
    # Find BingBong related types
    $bingBongTypes = $assembly.GetTypes() | Where-Object { $_.Name -like "*BingBong*" }
    Write-Host "`nFound $($bingBongTypes.Count) BingBong related types:" -ForegroundColor Cyan
    
    foreach ($type in $bingBongTypes) {
        Write-Host "  - $($type.FullName)" -ForegroundColor White
    }
    
    # Find Item related types
    $itemTypes = $assembly.GetTypes() | Where-Object { $_.Name -like "*Item*" } | Select-Object -First 5
    Write-Host "`nFound $($itemTypes.Count) Item related types (first 5):" -ForegroundColor Cyan
    
    foreach ($type in $itemTypes) {
        Write-Host "  - $($type.FullName)" -ForegroundColor White
    }
    
    # Find Player related types
    $playerTypes = $assembly.GetTypes() | Where-Object { $_.Name -like "*Player*" }
    Write-Host "`nFound $($playerTypes.Count) Player related types:" -ForegroundColor Cyan
    
    foreach ($type in $playerTypes) {
        Write-Host "  - $($type.FullName)" -ForegroundColor White
    }
    
} catch {
    Write-Host "Error loading DLL: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nInspection complete." -ForegroundColor Green
