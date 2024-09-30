#/usr/bin/bash
rm -rf ./releases
rm -rf ./bin/Release
mkdir -p ./releases/linux-x64-shared
dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true --self-contained false
cp -r ./bin/Release/net6.0/linux-x64/publish/* ./releases/linux-x64-shared
zip -r releases/linux-x64-shared.zip ./releases/linux-x64-shared
zip -r releases/fs24bot3.zip -j ./releases/linux-x64-shared/fs24bot3
