# Livly Gardener

リヴリーアイランドの水やり・木の実回収を自動化するWindowsアプリです。バージョン0.8.1。

0.8.1では、Hom Power不足通知の描画差による見逃しを修正しました。左下の「かえる」を押し、ホームに戻って自動停止します。実機でも一連の動作を確認済みです。

## ダウンロード

[インストーラーをダウンロード](https://github.com/daisan-me/LivlyGardener/releases/latest/download/LivlyGardener-Setup.exe)

Chromeがインストーラーを保留・削除する場合は、[ポータブル版（ZIP）をダウンロード](https://github.com/daisan-me/LivlyGardener/releases/latest/download/LivlyGardener-portable.zip)してください。ZIPを展開し、LivlyGardener.exeを起動します。どちらも公式リリースの同じ完成版です。

## ダウンロード警告が出る場合

ChromeのSafe BrowsingとWindowsセキュリティは無効にせず、まず公式リリースのファイルか確認してください。Safe Browsingを無効にしていると、Chromeはファイルを未確認として扱います。

PowerShellでインストーラーのSHA256を確認できます。ダウンロード先を指定してGet-FileHashを実行し、次の値と一致するか確認してください。

33d0796000e93f23dde525bc6d5871e2f21fe5c050562ac8f109bdc0b9f2c087

値が一致しない場合は実行せず、公式リリースから再取得してください。Windowsセキュリティの保護の履歴に具体的なマルウェア名がある場合は実行せず、Microsoftへ誤検知として報告してください。

## 使い方

1. BlueStacksの「設定 → 詳細設定 → Android Debug Bridge」を有効にします。
2. Setupを実行します。旧版は先に閉じてください。完了後アプリが起動し、デスクトップとスタートメニューにショートカットが作られます。
3. ゲームを縦向きのホーム画面にして「接続を確認」→「開始」を押します。
4. 停止は停止ボタンまたはF8です。

接続先は標準で127.0.0.1:5555です。異なる場合は「接続・設定」で接続先とADB実行ファイルを変更してください。BlueStacks付属ADBを使用します。

## 機能

- フレンドを巡回し、木の実・水滴のマークを検出してタップします。収穫成功時はOKを押します。
- 操作後の待機は1.0〜5.0秒、0.2秒刻みで設定・保存できます。初期値は4.0秒です。
- Hom Powerが足りませんを検出したら、左下の「かえる」を押し、帰還を確認して自動停止します。
- Androidの描画サイズに応じて座標を変換し、BlueStacksのWindowsウィンドウサイズに依存しません。
- 小型UI（本体820×620、100%表示時）と葉のアイコンを使用しています。

HPwrの数値は読み取りません。GP配り・デイリー・回復アイテムは対象外です。

## 動作環境・確認範囲

Windows 10/11 x64、.NET Framework 4.5以降、Windows PowerShell 5.1、BlueStacks 5。主な検証対象は9:16の縦画面です。実行中はゲームを手動操作しないでください。

実機でホームからの移動、水やり、収穫→OK→次の島への巡回、および不足通知→左下のかえる→ホーム帰還→自動停止を確認しました。通知の認識は360×640・900×1600・1080×1920の画像でもテスト済みです。

## 更新・削除

更新はアプリを閉じて新しいSetupを実行します。インストール先は%LOCALAPPDATA%/Programs/LivlyGardener、設定・ログ・認識用の一時画像は%LOCALAPPDATA%/LivlyGardenerPrototypeです。

ログや画像を外部送信しません。コード署名・自動更新はありません。本アプリは非公式です。リヴリーアイランド・BlueStacksの運営とは関係ありません。
