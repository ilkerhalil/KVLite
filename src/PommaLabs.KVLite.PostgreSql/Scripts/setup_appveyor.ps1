$env:PGUSER     = "postgres"
$env:PGPASSWORD = "Password12!"
$dbName         = "kvlite"

$setup = "$PSScriptRoot\kvl_cache_entries.sql"

iex "& `"C:\Program Files\PostgreSQL\9.6\bin\createdb`" $dbName"
iex "& `"C:\Program Files\PostgreSQL\9.6\bin\psql`" -d $dbName -f `"$setup`""
