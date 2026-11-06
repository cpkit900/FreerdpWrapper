# ==============================================================================
# build_xfreerdp_linux.sh
# Build script for xfreerdp on Linux (Ubuntu / Debian / WSL2)
# ==============================================================================

set -euo pipefail

echo "=========================================="
echo " FreeRDP X11 (xfreerdp) Build Script"
echo " Target OS: Ubuntu / Debian / WSL2"
echo "=========================================="

# 1. Install build prerequisites and dependencies
echo "[1/4] Installing dependencies via apt..."
sudo apt update
sudo apt install -y \
    build-essential \
    cmake \
    git \
    pkg-config \
    libssl-dev \
    libx11-dev \
    libxcursor-dev \
    libxext-dev \
    libxi-dev \
    libxinerama-dev \
    libxrandr-dev \
    libxrender-dev \
    libxv-dev \
    libxkbfile-dev \
    libasound2-dev \
    libcups2-dev \
    libxml2-dev \
    libavcodec-dev \
    libavutil-dev \
    libswscale-dev \
    libusb-1.0-0-dev \
    libpulse-dev

# 2. Setup build directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FREERDP_DIR="${SCRIPT_DIR}/Bin/FreeRDP"
BUILD_DIR="${FREERDP_DIR}/build_linux"
INSTALL_PREFIX="${SCRIPT_DIR}/Bin/FreeRDP/Compiled_linux"

echo "[2/4] Configuring CMake for xfreerdp..."
cmake -S "${FREERDP_DIR}" -B "${BUILD_DIR}" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_INSTALL_PREFIX="${INSTALL_PREFIX}" \
    -DWITH_X11=ON \
    -DWITH_CLIENT_COMMON=ON \
    -DWITH_SERVER=OFF \
    -DWITH_PULSE=ON \
    -DWITH_ALSA=ON \
    -DWITH_FFMPEG=ON \
    -DWITH_CUPS=ON

# 3. Build xfreerdp
echo "[3/4] Compiling xfreerdp..."
cmake --build "${BUILD_DIR}" --config Release -j"$(nproc)"

# 4. Install binaries
echo "[4/4] Installing to ${INSTALL_PREFIX}..."
cmake --install "${BUILD_DIR}"

echo "=========================================="
echo " xfreerdp build completed!"
echo " Binary located at: ${INSTALL_PREFIX}/bin/xfreerdp"
echo "=========================================="

