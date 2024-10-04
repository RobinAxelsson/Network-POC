@echo off

dotnet build --project .\src\peer\peer.csproj
start cmd /k "dotnet run --project .\src\Peer\Peer.csproj --no-build --no-restore -- 8080 8081 Lisa"
start cmd /k "dotnet run --project .\src\Peer\Peer.csproj --no-build --no-restore -- 8081 8080 Carl"