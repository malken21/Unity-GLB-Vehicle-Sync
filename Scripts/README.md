# Scripts ディレクトリ

本ディレクトリにはプロジェクト用ユーティリティスクリプトを格納する。

## create_sfx.sh

7-Zipを使用してWindowsビルドディレクトリを単一の自己解凍形式実行ファイル（SFX）にパッケージ化するシェルスクリプトである。

### 使用方法

本スクリプトは `./create_sfx.sh <build_dir> <target_exe> <output_filename>` の形式で実行する。引数 `<build_dir>` には `build/` 等のビルド出力ディレクトリを指定する。引数 `<target_exe>` にはビルドディレクトリ内の `MyGame.exe` 等のUnity実行ファイル名を指定する。引数 `<output_filename>` には生成される単一EXEの名称を指定する。

### 他プロジェクトでの再利用

他プロジェクトで再利用する場合は、まず `create_sfx.sh` をプロジェクトの `Scripts/` ディレクトリにコピーする。次に、GitHub Actionsワークフローにてビルドステップの後に本ファイルを呼び出す設定を追加する。具体的な設定例を以下に示す。

```yaml
- name: Create Single EXE
  run: |
    chmod +x Scripts/create_sfx.sh
    ./Scripts/create_sfx.sh build "YourApp.exe" "YourApp_Single.exe"
```
