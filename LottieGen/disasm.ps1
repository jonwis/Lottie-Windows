$a = gci -r after.dll | sort LastWriteTime | select -last 1
link -dump -linenumbers -disasm -symbols -out:$env:temp\disasm.txt $a.FullName
undname $env:temp\disasm.txt | out-file -encoding ascii testexe-readable.txt
