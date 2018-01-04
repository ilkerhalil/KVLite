$instance = "(local)\SQL2017"
$userName = "sa"
$password = "Password12!"
$dbName   = "kvlite"

$setup = Get-Content "$PSScriptRoot\kvl_cache_entries.sql"
$setup = $setup -replace "`t|`n|`r", ""

sqlcmd -S "$instance" -U "$userName" -P "$password" -Q "USE [master]; CREATE DATABASE [$dbName]; USE [$dbName]; $setup"
