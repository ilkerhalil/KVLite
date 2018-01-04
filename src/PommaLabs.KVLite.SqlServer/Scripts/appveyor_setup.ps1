$instance = "(local)\SQL2017"
$userName = "sa"
$password = "Password12!"

$script = "$PSScriptRoot\kvl_cache_entries.sql"

sqlcmd -S "$instance" -U "$userName" -P "$password" -I "$setup"
