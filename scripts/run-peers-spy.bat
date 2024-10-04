@echo off

dotnet build .\src\Peer\Peer.csproj
dotnet build .\src\NetWorkSpy\NetworkSpy.csproj
start cmd /k "dotnet run --project .\src\Peer\Peer.csproj --no-build --no-restore -- 8080 8081 Lisa"
start cmd /k "dotnet run --project .\src\Peer\Peer.csproj --no-build --no-restore -- 8081 8080 Carl"
start cmd /k "dotnet run --project .\src\NetWorkSpy\NetworkSpy.csproj --no-build --no-restore"