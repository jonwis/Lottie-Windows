gci *.cpp | %{ 
    $old = (get-content $_)
    $new = $old -replace '.Properties,','.Properties(),'
    if ($old.Length -ne $new.Length) { $new | Set-Content -FilePath $_  }
}
