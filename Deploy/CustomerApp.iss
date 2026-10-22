; CustomerApp インストーラー(Inno Setup 6)
;
; publish.ps1 から呼ばれる。単体で試すときは、Inno Setup で開いてコンパイルする
; (その場合は、先に dotnet publish で ..\artifacts\publish を作っておく)。
;
; 方針
;   ・.NET ランタイム込み(self-contained)で発行したフォルダを、そのまま配る。
;     → 配布先に .NET をインストールする手順が要らない。
;   ・設定ファイル appsettings.Local.json は、[Files] に含めない。
;     → 初回インストール時だけ、画面で聞いた接続先から作る。更新(上書きインストール)では触らない。
;   ・データベースの作成・更新は、インストーラーではなく、アプリが起動時に行う(マイグレーション)。

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

#define MyAppName "顧客管理"
#define MyAppExeName "CustomerApp.exe"

[Setup]
; AppId は、このアプリを識別する固定の値。変えると別のアプリ扱いになり、更新できなくなる。
AppId={{D2B1B2CE-ED4D-4373-81AE-0F33E4FE8DF4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\CustomerApp
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=CustomerApp-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardStyle=modern
; 起動中のアプリを検出して、閉じるよう案内する(更新時に exe が置き換えられない問題を避ける)
CloseApplications=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作る"; Flags: unchecked

[Files]
; 発行フォルダの中身を、まるごと配置する。
; appsettings.Local.json は、発行フォルダに無い(publish.ps1 が検査している)ので、ここで上書きされることはない。
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppName} のアンインストール"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} を起動する"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; インストーラーが作った設定ファイルは、アンインストールで消す。
; データベースの中身と、ログ(%LocalAppData%\CustomerApp\logs)は消さない。
Type: files; Name: "{app}\appsettings.Local.json"

[Code]
var
  ServerPage: TInputQueryWizardPage;

function LocalSettingsPath: String;
begin
  Result := ExpandConstant('{app}\appsettings.Local.json');
end;

procedure InitializeWizard;
begin
  ServerPage := CreateInputQueryPage(wpSelectDir,
    '接続先のデータベース',
    'このPCから接続する SQL Server を指定してください。',
    'Windows 認証(このPCにログインしているユーザー)で接続します。' + #13#10 +
    '後から変えるときは、インストール先の appsettings.Local.json を編集します。');

  ServerPage.Add('サーバー名(例: localhost\SQLEXPRESS、 SERVER01\SQLEXPRESS):', False);
  ServerPage.Add('データベース名:', False);

  ServerPage.Values[0] := 'localhost\SQLEXPRESS';
  ServerPage.Values[1] := 'CustomerDb';
end;

{ 更新(上書き)インストールで、設定ファイルが既にあるときは、この画面を出さず、設定も触らない }
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if PageID = ServerPage.ID then
    Result := FileExists(LocalSettingsPath);
end;

{ 接続文字列を壊す文字(; = " と改行など)を、入力から排除する }
function IsSafeValue(const S: String): Boolean;
begin
  Result := (Trim(S) <> '') and
            (Pos(';', S) = 0) and (Pos('=', S) = 0) and (Pos('"', S) = 0) and
            (Pos(#13, S) = 0) and (Pos(#10, S) = 0);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = ServerPage.ID then
  begin
    if not IsSafeValue(ServerPage.Values[0]) then
    begin
      MsgBox('サーバー名が空か、使えない文字(; = ")が含まれています。', mbError, MB_OK);
      Result := False;
    end
    else if (not IsSafeValue(ServerPage.Values[1])) or (Pos('\', ServerPage.Values[1]) > 0) then
    begin
      MsgBox('データベース名が空か、使えない文字(; = " \)が含まれています。', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure WriteLocalSettings;
var
  Conn, Json: String;
begin
  Conn := 'Server=' + Trim(ServerPage.Values[0]) +
          ';Database=' + Trim(ServerPage.Values[1]) +
          ';Trusted_Connection=True;TrustServerCertificate=True;';

  { JSON では、バックスラッシュを「\\」と書く決まり }
  StringChangeEx(Conn, '\', '\\', True);

  Json := '{' + #13#10 +
          '  "ConnectionStrings": {' + #13#10 +
          '    "Default": "' + Conn + '"' + #13#10 +
          '  }' + #13#10 +
          '}' + #13#10;

  SaveStringToFile(LocalSettingsPath, Json, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    { 既にあるファイルは上書きしない(更新インストールでは、利用者が編集した設定を守る) }
    if not FileExists(LocalSettingsPath) then
      WriteLocalSettings;
  end;
end;
