#/usr/bin/bash

# This is script for CI usage only
rm -rf releases/
rm -rf bin/x64/Release

mkdir releases/
mkdir releases/linux-x64-shared

dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true --self-contained false
cp -r ./fs24bot3/bin/Release/net6.0/linux-x64/publish/* releases/linux-x64-shared/