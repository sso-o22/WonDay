#!/bin/sh
# Cloudflare Pages 빌드 서버에는 .NET이 없어서 매 빌드마다 설치합니다.
# 실제 사용 중인 .NET 버전에 맞게 -c 뒤의 버전 숫자를 바꿔주세요 (예: 9.0).
curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh -c 9.0 -InstallDir ./dotnet
./dotnet/dotnet --version
./dotnet/dotnet publish -c Release -o output
