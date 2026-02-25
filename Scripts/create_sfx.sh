#!/bin/bash
# create_sfx.sh
# Usage: ./create_sfx.sh <build_dir> <exe_name> <output_name>

set -e

BUILD_DIR=$1
EXE_NAME=$2
OUTPUT_NAME=$3

if [ -z "$BUILD_DIR" ] || [ -z "$EXE_NAME" ] || [ -z "$OUTPUT_NAME" ]; then
    echo "Usage: $0 <build_dir> <exe_name> <output_name>"
    exit 1
fi

echo "[SFX] Installing dependencies..."
sudo apt-get update && sudo apt-get install -y p7zip-full wget curl

echo "[SFX] Creating 7z archive..."
cd "$BUILD_DIR"
7z a ../build.7z .
cd ..

echo "[SFX] Downloading SFX module..."
# Attempt multiple sources for 7zSD.sfx (Windows 32/64 bit compatible)
SFS_URLS=(
    "https://raw.githubusercontent.com/chrislake/7zip-sfx-builder/master/7zSD.sfx"
    "https://github.com/myfreeer/7z-sfx-extra/releases/download/v1.7.1-51/7zS2.sfx"
)

for url in "${SFS_URLS[@]}"; do
    if wget "$url" -O 7zSD.sfx; then
        echo "[SFX] Downloaded SFX module from $url"
        break
    fi
done

if [ ! -f "7zSD.sfx" ]; then
    echo "[SFX] Error: Failed to download SFX module."
    exit 1
fi

echo "[SFX] Generating config..."
cat <<EOF > sfx_config.txt
;!@Install@!UTF-8!
RunProgram="$EXE_NAME"
;!@InstallEnd@!
EOF

echo "[SFX] Packaging into $OUTPUT_NAME..."
cat 7zSD.sfx sfx_config.txt build.7z > "$OUTPUT_NAME"

# Cleanup
rm build.7z 7zSD.sfx sfx_config.txt

echo "[SFX] Done: $OUTPUT_NAME"
