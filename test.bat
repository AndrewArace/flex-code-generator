@echo off
.\fcg.exe your_assembly.dll  your_namespace -e -m -y -ds -di -b
::== generate a directory in the form 20101231 into %var%
for /F "tokens=2-4 delims=/- " %%A in ('date/T') do set var=%%C%%A%%B
rd /S/Q "%var%"
md "%var%"

::== DELETE ANY UNWANTED FILES


::== DONE DELETING
move *.as "%var%"
