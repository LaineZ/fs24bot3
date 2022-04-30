rmdir /S /Q releases
mkdir releases

rmdir /S /Q bin\Release

dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true --self-contained false

mkdir releases\linux-x64-shared

copy bin\Release\net6.0\linux-x64\publish releases\linux-x64-shared

del releases\linux-x64-shared.zip
del releases\fs24bot3.zip

powershell.exe -nologo -noprofile -command "Compress-Archive -Path releases\linux-x64-shared -DestinationPath releases\linux-x64-shared.zip"

REM this a special-incremental deploy
powershell.exe -nologo -noprofile -command "Compress-Archive -Path releases\linux-x64-shared\fs24bot3 -DestinationPath releases\fs24bot3.zip"

pause