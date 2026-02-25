---
description: "UnityプロジェクトのバージョンをBump upし、コミット、タグ付け、プッシュを行う手順"
---
# Unity バージョンアップ自動化スキル

このスキルは、Unityプロジェクトの `bundleVersion` をインクリメントし、Gitのコミット、タグ作成、プッシュの一連の作業を自動化します。

## 要求される前提条件

- ユーザーに現在のバージョンと新バージョンの案を提示し、合意を得ること
- プロジェクトルートで実行すること

## 手順

1. **現在のバージョンの確認**
   - ツール: `grep_search` (`bundleVersion:`)
   - 対象: `ProjectSettings/ProjectSettings.asset`
2. **バージョンの書き換え**
   - ツール: `replace_file_content`
   - 対象: `ProjectSettings/ProjectSettings.asset` の `bundleVersion` 行を更新する
3. **コミットの作成**
   - ツール: `run_command`
   - コマンド: `git add ProjectSettings/ProjectSettings.asset` と `git commit -m "Bump version to vX.Y.Z"` を実行する（X.Y.Z は新しいバージョン）
4. **タグの作成とプッシュ**
   - ツール: `run_command`
   - コマンド: `git tag vX.Y.Z` と `git push origin main --tags` を実行する
