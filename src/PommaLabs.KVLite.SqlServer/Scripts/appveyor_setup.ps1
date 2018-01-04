$instance = "(local)\SQL2017"
$userName = "sa"
$password = "Password12!"

$setup = Get-Content "$PSScriptRoot\kvl_cache_entries.sql"
$setup = $setup -replace "`t|`n|`r", ""

sqlcmd -S "$instance" -U "$userName" -P "$password" -Q "$setup"
