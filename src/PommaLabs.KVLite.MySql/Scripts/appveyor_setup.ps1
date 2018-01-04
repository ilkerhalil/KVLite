$userName      = "root"
$env:MYSQL_PWD = "Password12!"
$dbName        = "kvlite"

$setup = Get-Content "$PSScriptRoot\kvl_cache_entries.sql"
$setup = $setup -replace "`t|`n|`r", ""

iex "& `"C:\Program Files\MySQL\MySQL Server 5.7\bin\mysql`" -e `"create database $dbName; use $dbName; $setup`" --user=$userName"
