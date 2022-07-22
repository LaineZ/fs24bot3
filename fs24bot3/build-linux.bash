#/usr/bin/bash
rm -rf releases/
rm -rf bin/x64/Release

mkdir releases/
mkdir releases/linux-x64-shared

dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true --self-contained false

cp -r bin/Release/net6.0/linux-x64/publish releases/linux-x64-shared/

zip -r releases/linux-x64-shared.zip releases/linux-x64-shared
# this a special-incremental deploy
zip -9 -j releases/fs24bot3.zip releases/linux-x64-shared/publish/fs24bot3