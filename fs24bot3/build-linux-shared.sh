#/bin/bash
dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true --self-contained false
zip -D -9 bin/Release/netcoreapp3.1/linux-x64/publish/fs24bot3.zip bin/Release/netcoreapp3.1/linux-x64/publish/fs24bot3