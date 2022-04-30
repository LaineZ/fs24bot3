mkdir releases

rmdir /S /Q bin\Release
rmdir /S /Q releases\linux-x64-bundle

dotnet publish -c Release -p:Platform="x64" -r linux-x64 /p:PublishSingleFile=true

mkdir releases\linux-x64-bundle

copy static releases\linux-x64-bundle
copy bin\x64\Release\net6.0\linux-x64\publish releases\linux-x64-bundle

del releases\linux-x64-bundle.zip

powershell.exe -nologo -noprofile -command "Compress-Archive -Path releases\linux-x64-bundle -DestinationPath releases\linux-x64-bundle.zip"

pause