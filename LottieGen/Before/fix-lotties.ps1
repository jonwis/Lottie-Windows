Write-Host "Running Fix-Lotties.ps1"
gci *.cpp | %{ 
    $old = (get-content $_)
    $new = $old -replace '\.Properties\b','.Properties()'
    $new = $new -replace '\.Properties\(\)\(\)','.Properties()'

    #Write-Host $_ "$old".Length "$new".Length
    if ("$old".Length -ne "$new".Length) { Write-Host "Fixing $_" ; Set-Content $_ -Value $new }
}
