$env:PGUSER     = "postgres"
$env:PGPASSWORD = "Password12!"
$env:PGOPTIONS  = "--client-min-messages=warning"
$dbName         = "kvlite"

$setup = "$PSScriptRoot\kvl_cache_entries.sql"

iex "& `"C:\Program Files\PostgreSQL\9.6\bin\createdb`" $dbName"
iex "& `"C:\Program Files\PostgreSQL\9.6\bin\psql`" -q -d $dbName -f `"$setup`""
