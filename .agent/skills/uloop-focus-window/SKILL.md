---
name: uloop-focus-window
description: "uloop CLIを介してUnityエディタウィンドウを前面に表示する。使用目的: (1) スクリーンショット撮影前にUnityエディタをフォーカスする、(2) 視覚的な確認のためにUnityウィンドウを表示させる、(3) ユーザー操作のためにUnityを最前面に移動する。"
---

# uloop focus-window

Unityエディタウィンドウを前面に表示する。

## 使用方法

```bash
uloop focus-window
```

## パラメータ

なし。

## 例

```bash
# Unityエディタをフォーカス
uloop focus-window
```

## 出力

ウィンドウがフォーカスされたことを確認する JSON を返す。

## 注意事項

- 対象ウィンドウが表示されていることを確認するため、 `uloop capture-unity-window` の前に実行すると有用である
- メインのUnityエディタウィンドウを最前面に表示する
