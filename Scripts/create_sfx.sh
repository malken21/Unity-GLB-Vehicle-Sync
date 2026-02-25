#!/bin/bash
# create_sfx.sh
# 使用法: ./create_sfx.sh <ビルドディレクトリ> <実行ファイル名> <出力ファイル名>

set -e

BUILD_DIR=$1
EXE_NAME=$2
OUTPUT_NAME=$3

if [ -z "$BUILD_DIR" ] || [ -z "$EXE_NAME" ] || [ -z "$OUTPUT_NAME" ]; then
    echo "使用法: $0 <ビルドディレクトリ> <実行ファイル名> <出力ファイル名>"
    exit 1
fi

echo "依存関係をインストール中..."
sudo apt-get update && sudo apt-get install -y p7zip-full wget curl

echo "7zアーカイブを作成中..."
cd "$BUILD_DIR"
7z a ../build.7z .
cd ..

echo "SFXモジュールをダウンロード中..."
wget -q https://www.7-zip.org/a/lzma2409.7z -O lzma.7z
7z e lzma.7z bin/7zSD.sfx -y

if [ ! -f "7zSD.sfx" ]; then
    echo "エラー: SFXモジュールのダウンロードに失敗。"
    exit 1
fi

echo "設定ファイルを生成中..."
cat <<EOF > sfx_config.txt
;!@Install@!UTF-8!
RunProgram="$EXE_NAME"
;!@InstallEnd@!
EOF

echo "$OUTPUT_NAME へのパッケージングを実行中..."
cat 7zSD.sfx sfx_config.txt build.7z > "$OUTPUT_NAME"

# クリーンアップ
rm -f build.7z 7zSD.sfx sfx_config.txt lzma.7z

echo "完了: $OUTPUT_NAME"
