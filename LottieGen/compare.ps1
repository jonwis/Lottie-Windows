$a = gci -r after.dll | sort LastWriteTime | select -last 1
$b = gci -r before.dll | sort LastWriteTime | select -last 1
$d = ($a.Length - $b.Length)
$p = ($d / $b.Length) * 100

Write-Host $a.FullName $a.Length
Write-Host $b.FullName $b.Length
Write-Host Delta $d change $p
