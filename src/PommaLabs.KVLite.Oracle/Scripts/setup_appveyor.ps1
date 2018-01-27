git clone https://github.com/apatitejs/appveyor-oracle-setup
npm install ./appveyor-oracle-setup/
md temp
node ./appveyor-oracle-setup/app.js
7z.exe e "temp/ora11gr2setup.zip.001" -o"orasetup"
cd..
