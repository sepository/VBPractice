# CustomerApp 配布手順

> **この手順書とスクリプトは、作成環境では実際に実行できていません**(Windows・.NET SDK・Inno Setup が無いため)。
> 最初の1回は、必ず手元で「発行 → 別のPC(または仮想マシン)にインストール → 起動確認」まで通してください。
> 食い違いが出たら、そのメッセージを教えてもらえれば直します。

## 1. 配布方式の選び方

| 方式 | 配布先に .NET は | 配布物の大きさ | 向いている場面 |
|---|---|---|---|
| **A. self-contained フォルダ + インストーラー(既定)** | 不要 | 大きめ(WinForms で100MB前後) | 配布先のPCの状態を問わず動かしたい。**今回の既定** |
| B. framework-dependent + インストーラー | .NET 10 Desktop Runtime が必要 | 小さい | 全PCに、ランタイムを管理者が入れている |
| C. 単一ファイル(PublishSingleFile) | A / B に準じる | 1つの exe | 配るのが exe 1個で済ませたいとき |

既定を A にした理由:

- 配布先で「.NET が入っていない」問題が起きない。ランタイムの有無を判定するインストーラーの処理(検出ロジック)が要らない。
- 設定ファイル(`appsettings.json` / `appsettings.Local.json`)が、exe の横に普通のファイルとして置かれる。このアプリは `AppContext.BaseDirectory` から設定を読むので、単一ファイルにすると、設定ファイルの扱いに追加の注意が要る。
- 欠点は大きさと、ランタイムのセキュリティ更新が自動では入らないこと(更新するには、アプリを作り直して配り直す)。

B にしたい場合: `publish.ps1` の `--self-contained true` を `false` にし、配布先に **.NET 10 Desktop Runtime(x64)** を入れておく。
.NET 10 は LTS で、サポートは 2028-11-14 まで。.NET 8 / 9 は 2026-11-10 で終わる(今日 2026-10-22 の時点で、残り約3週間)。

## 2. プロジェクトの設定(1回だけ)

### 2-1. 設定ファイルの「発行ディレクトリにコピー」

「発行ディレクトリにコピー」(`CopyToPublishDirectory`)は、Visual Studio のプロパティ画面には出ないことがある(「出力ディレクトリにコピー」だけが出る)。
その場合は画面では設定できないので、`.vbproj` を直接編集する。

1. ソリューションエクスプローラーで、プロジェクト(`CustomerApp`)を右クリック →「プロジェクト ファイルの編集」。
2. `</Project>` の直前に、下の `ItemGroup` を貼り付けて保存する。

`Update` は、既にある項目の設定だけを上書きする書き方。目的は次のとおり。

| ファイル | 出力ディレクトリにコピー | 発行ディレクトリにコピー |
|---|---|---|
| `appsettings.json` | 新しい場合はコピーする | **新しい場合はコピーする** |
| `appsettings.Local.json` | 新しい場合はコピーする(開発用) | **コピーしない** |
| `appsettings.Local.json.example` | コピーしない | **コピーしない** |

ファイルが `None` ではなく `Content` として入っているなら、`None` を `Content` に読み替える。

```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
  </None>
  <None Update="appsettings.Local.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <CopyToPublishDirectory>Never</CopyToPublishDirectory>
  </None>
  <None Update="appsettings.Local.json.example">
    <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    <CopyToPublishDirectory>Never</CopyToPublishDirectory>
  </None>
</ItemGroup>
```

`appsettings.Local.json` を発行物に入れてはいけない理由:
開発PCの接続先が、そのまま全員に配られてしまうため。`publish.ps1` は、混入していたら止まる。

### 2-2. バージョンと名前

```xml
<PropertyGroup>
  <AssemblyName>CustomerApp</AssemblyName>   <!-- exe の名前。インストーラーと publish.ps1 は CustomerApp.exe を前提にしている -->
  <Version>1.0.0</Version>                   <!-- publish.ps1 の -Version で上書きされる -->
</PropertyGroup>
```

### 2-3. マイグレーションSQLが埋め込まれているか

`Migrations\V*.sql` は、exe に**埋め込み**(EmbeddedResource)で入っている。配布先に SQL ファイルを置く必要はない。
既存の単体テスト `EmbeddedMigrationsTests` が通っていれば、設定は正しい。

### 2-4. Git に入れないもの

`.gitignore` に追記:

```
artifacts/
appsettings.Local.json
```

## 3. 発行とインストーラーの作成

### 準備(開発PCに1回)

- **.NET 10 SDK**(今開発に使っているもの)
- **Inno Setup 6**(<https://jrsoftware.org/isdl.php>)。`ISCC.exe`(コマンドライン版コンパイラ)が一緒に入る。
- PowerShell(Windows 標準のものでよい)

### 実行

ソリューションのフォルダで:

```powershell
.\Deploy\publish.ps1 -Version 1.0.0
```

実行ポリシーで止まる場合:

```powershell
powershell -ExecutionPolicy Bypass -File .\Deploy\publish.ps1 -Version 1.0.0
```

| オプション | 意味 |
|---|---|
| `-SkipTests` | テストを省く。結合テストは SQL Server(`CustomerDb_Test`)に接続するので、SQL Server が無いPCでは付ける |
| `-SkipInstaller` | 発行フォルダ(`artifacts\publish`)だけ作る。Inno Setup が無いとき |
| `-Runtime win-arm64` | ARM の Windows 向け(その場合は .iss の `ArchitecturesAllowed` も要変更) |

成果物:

- `artifacts\publish\` … 発行フォルダ(これをコピーするだけでも動く。ただし設定ファイルは自分で作る)
- `artifacts\installer\CustomerApp-Setup-1.0.0.exe` … 配るのはこれ

スクリプトが自動で確認すること: exe と `appsettings.json` がある / ローカル設定が混入していない / 接続文字列がある / パスワードが書かれていない。

## 4. インストーラーの動き

1. 管理者権限で起動(`Program Files` に入れるため)。
2. インストール先を選ぶ(既定: `C:\Program Files\CustomerApp`)。
3. **接続先の画面**で、サーバー名とデータベース名を入力 → `appsettings.Local.json` が作られる。
   - 既定値: `localhost\SQLEXPRESS` / `CustomerDb`。Windows 認証で接続する。
   - `; = "` を含む入力は弾く(接続文字列が壊れるため)。
4. **更新(上書きインストール)**:`appsettings.Local.json` が既にあれば、画面を出さず、設定にも触らない。
5. アンインストール:アプリと、インストーラーが作った設定ファイルを消す。**データベースとログは消さない**。

接続先を後から変えるには、`C:\Program Files\CustomerApp\appsettings.Local.json` を**管理者権限のエディタ**で編集する
(`Program Files` 配下は、一般ユーザーは書き込めない)。JSON なので、バックスラッシュは `\\` と書く。

サイレントインストール(画面なし)は `CustomerApp-Setup-1.0.0.exe /VERYSILENT` だが、
この場合、接続先の画面は出ず、`appsettings.Local.json` は**作られない**(`appsettings.json` の既定の接続先で動く)。
自動配布をするなら、設定ファイルを別に配置するか、環境変数 `CUSTOMERAPP_ConnectionStrings__Default` を使う。

## 5. 配布先PCの前提

### SQL Server

- 接続先の SQL Server(Express でよい)が動いていること。
- **同じPCに SQL Server がある場合**:`localhost\SQLEXPRESS` で接続できる。
- **別のサーバーにある場合**:サーバー側で、TCP/IP を有効にし、SQL Server Browser(名前付きインスタンスの場合)を起動し、ファイアウォールを開ける。
- **Windows 認証**:アプリを使う Windows ユーザーに、SQL Server のログインを作る(ドメイン環境なら、ドメインユーザー/グループで)。

### 必要な権限(どちらかを選ぶ)

| 運用 | 設定 | 利用者に必要な権限 |
|---|---|---|
| **自動(小規模・既定)** | `AutoCreate=true` / `AutoMigrate=true` | DB作成(`dbcreator`)、テーブル作成・変更ができる権限 |
| **管理者が管理(推奨・複数人利用)** | `AutoCreate=false` / `AutoMigrate=false` | `db_datareader` / `db_datawriter` だけでよい。DB作成とテーブル作成は、管理者が先にアプリを起動して行う |

自動運用でも、**日常の利用者に強い権限を与え続けるのは避ける**のが望ましい。
後者にする場合、`appsettings.Local.json` に次を足す:

```json
{
  "ConnectionStrings": {
    "Default": "Server=SERVER01\\SQLEXPRESS;Database=CustomerDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Database": {
    "AutoCreate": false,
    "AutoMigrate": false
  }
}
```

> 注意:`AutoMigrate=false` のとき、DBが古い(未適用のスクリプトがある)と、アプリは「データベースが最新ではありません」と案内して**起動を中止**する(`DatabaseInitializer` の実装を確認済み)。
> 更新のたびに、管理者が新しい `V###__*.sql` を適用してから、アプリを配る。
> なお、手作業で適用するときは、アプリと同じく `dbo.SchemaVersions` に履歴(チェックサム込み)を残す必要がある。
> 履歴を手で入れるのは間違えやすいので、**管理者のPCで一度 `AutoMigrate=true` のアプリを起動して適用する**のが確実。

### 接続の暗号化について

接続文字列に `TrustServerCertificate=True` を入れている。これは**証明書の検証を省略する**設定で、
暗号化はされるが、なりすましの検出はできない。社内LANで、信頼できるサーバーに接続するなら実用上は許容されることが多いが、
インターネット越しや、重要なデータでは、サーバーに正式な証明書を入れ、この設定を外すこと。

## 6. 動作確認チェックリスト(配布先のPCで)

- [ ] インストーラーが最後まで進む(SmartScreen の警告が出る場合は 7 を参照)
- [ ] スタートメニューから起動できる
- [ ] 初回起動で、DB・テーブルが作られる(自動運用の場合)/ 管理者運用なら、エラーなく一覧が出る
- [ ] 顧客の追加・更新・削除ができる
- [ ] 注文と明細の登録ができる(トランザクション)
- [ ] CSV / Excel 出力ができる
- [ ] ログが `%LocalAppData%\CustomerApp\logs` に出ている
- [ ] 接続先を間違えたとき(`appsettings.Local.json` を壊す/サーバーを止める)、分かりやすいメッセージが出て終了する
- [ ] **更新**:新しいバージョンのインストーラーを上書きで実行 → 接続先の画面は出ず、設定が保たれ、データも残っている
- [ ] アンインストール後、データベースとログが残っている

## 7. 運用上の注意

- **更新前にDBをバックアップする**(アプリの「バックアップ / 復元」画面で取れる。下の「バックアップ・復元の注意」を参照)。マイグレーションは、一度適用すると元に戻せない(ロールバック用のスクリプトは無い)。
- **V005(メールアドレスの一意インデックス)の適用前に、重複を確認する**。既存の顧客に、同じメールアドレスが2件以上あると、V005 は失敗し、アプリは起動しない(データは変更されない)。起動時のメッセージにある SQL(`SELECT Email, COUNT(*) ... HAVING COUNT(*) > 1`)で重複を確認し、不要な顧客の削除、またはメールアドレスの修正をしてから、もう一度起動する。`AutoMigrate=false` の運用では、更新前に、管理者がこの SQL を実行しておくとよい。
- **古いアプリで新しいDBに接続**すると、アプリが起動を拒否する(DBがアプリより新しいため)。これは意図した安全装置。
- **署名の無い exe は、SmartScreen に警告される**ことがある(「WindowsによってPCが保護されました」)。社内配布なら、「詳細情報 → 実行」で進める、または社内の共有フォルダから配る。外部に配るなら、コード署名証明書で署名する。
- **ランタイム更新**:self-contained では、.NET のセキュリティ修正を入れるたびに、`publish.ps1` を再実行して配り直す。
- **バージョンの付け方**:`1.0.0` → 機能追加は `1.1.0`、修正だけなら `1.0.1`。マイグレーションを足したら、必ずバージョンを上げる。

### バックアップ・復元の注意

アプリの「バックアップ / 復元」画面は、SQL Server の `BACKUP DATABASE` / `RESTORE DATABASE` を実行する。

- **ファイルを読み書きするのは SQL Server**。指定するパスは「SQL Server が動いている PC 上」のパスで、権限も SQL Server のサービスアカウント(SQLEXPRESS なら `NT Service\MSSQL$SQLEXPRESS`)のもの。Documents やデスクトップを指定すると、権限エラーになりやすい。初期値の「既定のバックアップ先」(SQL Server が決めているフォルダ)なら、確実に書ける。
- **必要な権限**:バックアップは `db_backupoperator`、復元は `dbcreator`(または `sysadmin`)。配布先で `db_datareader` / `db_datawriter` だけのユーザーは、この画面を使えない(エラーメッセージで案内される)。
- **復元の流れ**:ファイルを検査 → 現在のDBを自動でバックアップ(`…_復元前_日時.bak`) → 他の接続を切る → 復元。復元後は、アプリを閉じるので、起動し直す。
- **復元したバックアップが古い**場合は、起動時のマイグレーションで最新の構造に上がる(`AutoMigrate=true` のとき)。**新しい**場合は、「DBがアプリより新しい」として起動を拒否する。
- **復元前の自動バックアップは、自動では消えない**。容量が気になるときは、定期的に消す。
- 対応するのは、データファイル1個+ログファイル1個の、完全バックアップだけ(差分・ログのバックアップは不可)。
- バックアップファイルには、顧客の個人情報がそのまま入っている。保存先のフォルダの権限と、持ち出しに注意する。
- 定期バックアップ(毎晩など)は、この画面ではなく、SQL Server の側(SQL Server Agent、または Windows のタスクスケジューラ+`sqlcmd`)で組む。EXPRESS には Agent が無い。

## 8. 検証できていない点(最初の1回で確認してください)

1. `Japanese.isl`:Inno Setup 6 に同梱されている前提。無ければコンパイルエラーになるので、`[Languages]` の行を消せば英語UIで動く。
2. `ArchitecturesAllowed=x64compatible`:Inno Setup 6.3 以降の書き方。古い 6.x では `x64` に変える。
3. self-contained 発行に、SQL Server 用のネイティブライブラリ(`Microsoft.Data.SqlClient.SNI.dll`)が入ること(通常は `runtimes\win-x64\native\` に入る)。発行フォルダを見て、無ければ教えてください。
4. `CloseApplications=yes` で、起動中のアプリが検出されること。
5. `db_datareader` / `db_datawriter` だけで、全画面(特に履歴テーブルの読み取り)が動くか。足りない権限があれば、メッセージを教えてください。
6. 単一ファイル発行(方式C)は、今回のスクリプトでは試していない。
7. バックアップ・復元(`BackupService`):この画面は、まだ実機で動かしていない。特に、復元後に `ALTER DATABASE ... SET MULTI_USER` が正しく戻ること、`RESTORE ... WITH MOVE` のパス指定、`SERVERPROPERTY('InstanceDefaultBackupPath')`(SQL Server 2019 以降)が取れること。
