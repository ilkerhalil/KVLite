$env:PGUSER = "postgres"
$env:PGPASSWORD = "Password12!"
$env:PGOPTIONS = "--client-min-messages=warning"

$setup = "$PSScriptRoot\kvl_cache_entries.sql"

Invoke-Expression "& `"C:\Program Files\PostgreSQL\9.6\bin\psql`" -q -d postgres -f `"$setup`""
