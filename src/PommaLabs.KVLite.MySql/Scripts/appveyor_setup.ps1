$userName      = "root"
$env:MYSQL_PWD = "Password12!"
$dbName        = "kvlite"
iex "& `"C:\Program Files\MySQL\MySQL Server 5.7\bin\mysql`" -e `"create database $dbName;`" --user=$userName"
