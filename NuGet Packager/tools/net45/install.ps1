###############################################################################
#
# install.ps1 --
#
# Written by Joe Mistachkin.
# Released to the public domain, use at your own risk!
#
###############################################################################

param($installPath, $toolsPath, $package, $project)

$mainDirectory = "KVLite"
$platformNames = "x86", "x64"
$sqlite = "SQLite.Interop.dll"
$snappy = "Snappy.Interop.dll"
$crc = "Crc32C.Interop.dll"
$propertyName = "CopyToOutputDirectory"

foreach($platformName in $platformNames) {
  $folder = $project.ProjectItems.Item($mainDirectory)
  
  if ($folder -eq $null) {
    continue
  }

  $folder = $folder.ProjectItems.Item($platformName)

  if ($folder -eq $null) {
    continue
  }

  $item = $folder.ProjectItems.Item($sqlite)
  if ($item -eq $null) {
    continue
  }
  $property = $item.Properties.Item($propertyName)
  if ($property -ne $null) {
    $property.Value = 2
  }

  $item = $folder.ProjectItems.Item($snappy)
  if ($item -eq $null) {
    continue
  }
  $property = $item.Properties.Item($propertyName)
  if ($property -ne $null) {
    $property.Value = 2
  }

  $item = $folder.ProjectItems.Item($crc)
  if ($item -eq $null) {
    continue
  }
  $property = $item.Properties.Item($propertyName)
  if ($property -ne $null) {
    $property.Value = 2
  }
}
