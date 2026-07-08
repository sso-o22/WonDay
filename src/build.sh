#!/bin/sh
# Cloudflare 빌드 서버에는 .NET이 없어서 매 빌드마다 설치합니다.
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 8.0 -InstallDir ./dotnet
./dotnet/dotnet --version
./dotnet/dotnet publish -c Release -o output
