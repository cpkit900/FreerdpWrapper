$ErrorActionPreference = "Stop"

$baseDir = "c:\Users\Junnu\Freerdp Wrapper"
$releaseDir = "$baseDir\FreeRdpWrapper_Release"
$publishDir = "$baseDir\PublishOutput"
$zipFile = "$baseDir\FreeRdpWrapper_Release.zip"

Write-Host "Cleaning up old directories..."
if (Test-Path $releaseDir) { Remove-Item -Recurse -Force $releaseDir }
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (Test-Path $zipFile) { Remove-Item -Force $zipFile }

Write-Host "Publishing .NET App..."
dotnet publish "$baseDir\FreeRdpWrapper.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDir

Write-Host "Assembling release folder..."
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
Copy-Item "$publishDir\FreeRdpWrapper.exe" -Destination $releaseDir -Force

$binDir = "$releaseDir\Bin\FreeRDP"
New-Item -ItemType Directory -Force -Path $binDir | Out-Null
Copy-Item "$baseDir\Bin\FreeRDP\Compiled\bin\*" -Destination $binDir -Recurse -Force

Write-Host "Zipping release..."
Compress-Archive -Path "$releaseDir\*" -DestinationPath $zipFile -Force

Write-Host "Release created successfully at $zipFile"
