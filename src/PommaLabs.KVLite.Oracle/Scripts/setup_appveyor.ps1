appveyor DownloadFile https://github.com/symfony/binary-utils/releases/download/v0.1/OracleXE112_Win64.zip
7z x OracleXE112_Win64.zip -y > $null
DISK1\setup.exe /s /f1"DISK1\response\OracleXE-install.iss"
