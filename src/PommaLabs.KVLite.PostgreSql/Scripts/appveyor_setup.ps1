$env:PGUSER     = "postgres"
$env:PGPASSWORD = "Password12!"
$dbName         = "kvlite"
iex "& `"C:\Program Files\PostgreSQL\9.6\bin\createdb`" $dbName"
