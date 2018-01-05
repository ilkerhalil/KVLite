$userName = "root"
$env:MYSQL_PWD = "Password12!"

$setup = Get-Content "$PSScriptRoot\kvl_cache_entries.sql"
$setup = $setup -replace "`t|`n|`r", ""

Invoke-Expression "& `"C:\Program Files\MySQL\MySQL Server 5.7\bin\mysql`" -e `"$setup`" --user=$userName"
