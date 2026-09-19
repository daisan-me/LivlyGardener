# Livly Gardener

リヴリーアイランドの水やり・木の実回収を自動化するWindowsアプリです。バージョン0.8.1。

0.8.1では、Hom Power不足通知の描画差による見逃しを修正しました。左下の「かえる」を押し、ホームに戻って自動停止します。実機でも一連の動作を確認済みです。

## ダウンロード

**[インストーラーをダウンロード](https://github.com/daisan-me/LivlyGardener/releases/latest/download/LivlyGardener-Setup.exe)**

`LivlyGardener-Setup.exe` 1本で導入できます。Python・開発ツールは不要です。BlueStacks 5とゲームは別途必要です。

## 使い方

1. BlueStacksの「設定 → 詳細設定 → Android Debug Bridge」を有効にします。
2. Setupを実行します。旧版は先に閉じてください。完了後アプリが起動し、デスクトップとスタートメニューにショートカットが作られます。
3. ゲームを縦向きのホーム画面にして「接続を確認」→「開始」を押します。
4. 停止は停止ボタンまたはF8です。

接続先は標準で `127.0.0.1:5555`。異なる場合は「接続・設定」で接続先とADB実行ファイルを変更してください。BlueStacks付属ADBを使用します。

## 機能

- フレンドを巡回し、木の実・水滴のマークを検出してタップします。収穫成功時はOKを押します。
- 操作後の待機は1.0〜5.0秒、0.2秒刻みで設定・保存できます。初期値は4.0秒。ゲームへの反映のため4.0秒以上を推奨します。
- 「Hom Powerが足りません」を検出したら、設定時間の待機後に「かえる」を押し、帰還を確認して自動停止します。
- Androidの描画サイズに応じて座標を変換し、BlueStacksのWindowsウィンドウサイズに依存しません。
- 小型UI（本体820×620、100%表示時）と葉のアイコン。

HPwrの数値は読み取りません。「操作完了」はタップと待機を終えた数で、サーバー側の受付を直接確認した数ではありません。GP配り・デイリー・回復アイテムは対象外です。

## 動作環境・確認範囲

Windows 10/11 x64、.NET Framework 4.5以降、Windows PowerShell 5.1、BlueStacks 5。補助的な文字認識にはWindowsの英語OCR機能を使用します。認識画像とOCR補助スクリプトは同梱済みです。

主な検証対象は9:16の縦画面です。実行中はゲームを手動操作しないでください。背景・通信・PC性能によって速度や認識結果が変わります。読めない島は再試行後にスキップし、画面自体を確認できない場合は停止します。巡回上限・60分経過・10回連続の未処理でも終了します。

実機でホームからの移動、水やり、収穫→OK→次の島への巡回、および「不足通知→左下のかえる→ホーム帰還→自動停止」を確認しました。通知の認識は360×640・900×1600・1080×1920の画像でもテスト済みです。

## 更新・削除

更新はアプリを閉じて新しいSetupを実行します。

- インストール先：`%LOCALAPPDATA%\Programs\LivlyGardener`
- 設定・ログ・認識用の一時画像：`%LOCALAPPDATA%\LivlyGardenerPrototype`

ログや画像を外部送信しません。削除時は上記フォルダーとデスクトップ・スタートメニューのショートカットを削除してください。コード署名・自動更新はありません。

## 開発

Windows標準の.NET Frameworkコンパイラーを使用します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

`dist` にインストーラーとポータブルZIPを出力します。`preview.ps1 -OutputDirectory <path>` でUI画像を生成できます。個人情報を含むゲーム全画面テスト画像・実行ログは公開物に含めません。

本アプリは非公式です。リヴリーアイランド・BlueStacksの運営とは関係ありません。
