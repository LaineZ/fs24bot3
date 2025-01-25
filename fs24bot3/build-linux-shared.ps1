Remove-Item -Recurse -Force ./releases
Remove-Item -Recurse -Force ./bin/Release

New-Item -ItemType Directory -Path ./releases
New-Item -ItemType Directory -Path ./releases\linux-x64-shared

dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true --self-contained false

Copy-Item -Recurse -Path .\bin\Release\net8.0\linux-x64\publish\* -Destination .\releases\linux-x64-shared
Compress-Archive -Path .\releases\linux-x64-shared -DestinationPath releases\linux-x64-shared.zip
Compress-Archive -Path .\releases\linux-x64-shared\fs24bot3 -DestinationPath releases\fs24bot3.zip
Read-Host -Prompt "Press Enter to continue"