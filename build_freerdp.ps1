param (
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

$baseDir = "C:\Users\Junnu\Freerdp Wrapper"
$freeRdpDir = "$baseDir\Bin\FreeRDP"
$buildDir = "$freeRdpDir\build"
$compiledDir = "$freeRdpDir\Compiled"
$vcpkgToolchain = "C:/Users/Junnu/vcpkg/scripts/buildsystems/vcpkg.cmake"

Write-Host "=========================================="
Write-Host " Building FreeRDP "
Write-Host "=========================================="

if ($Clean -and (Test-Path $buildDir)) {
    Write-Host "Cleaning build directory..."
    Remove-Item -Recurse -Force $buildDir
}

if (!(Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
}

Write-Host "Configuring CMake..."
cmake -S $freeRdpDir -B $buildDir `
      -G "Visual Studio 17 2022" -A x64 `
      -DCMAKE_TOOLCHAIN_FILE="C:/Users/Junnu/vcpkg/scripts/buildsystems/vcpkg.cmake" `
      -DCMAKE_BUILD_TYPE=Release `
      -DCMAKE_INSTALL_PREFIX=$compiledDir `
      -DCMAKE_INTERPROCEDURAL_OPTIMIZATION=ON `
      -DFREERDP_UNIFIED_BUILD=ON `
      -DBUILD_SHARED_LIBS=ON `
      -DWITH_SERVER=OFF `
      -DWITH_MEDIA_FOUNDATION=OFF `
      -DWITH_OPENH264=ON

if ($LASTEXITCODE -ne 0) {
    Write-Error "CMake configuration failed."
}

Write-Host "Building FreeRDP Release..."
cmake --build $buildDir --config Release --parallel

if ($LASTEXITCODE -ne 0) {
    Write-Error "CMake build failed."
}

Write-Host "Installing to Compiled/ directory..."
cmake --install $buildDir --config Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "CMake install failed."
}

Write-Host "Copying vcpkg dependencies into Compiled/bin..."
$vcpkgBin = "C:\Users\Junnu\vcpkg\installed\x64-windows\bin"
if (Test-Path $vcpkgBin) {
    Copy-Item "$vcpkgBin\*.dll" -Destination "$compiledDir\bin\" -Force
}

Write-Host "=========================================="
Write-Host " FreeRDP Build Complete!"
Write-Host " Binaries located at: $compiledDir"
Write-Host "=========================================="
