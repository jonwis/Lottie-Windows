gci *.cpp | %{ (get-content $_) -replace '.Properties,','.Properties(),' | set-content $_ }
