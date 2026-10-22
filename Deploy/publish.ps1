<#
.SYNOPSIS
  CustomerApp を発行(publish)し、インストーラー(Inno Setup)を作る。

.DESCRIPTION
  1. 単体テスト・結合テストを実行する(-SkipTests で省略)
  2. dotnet publish で、.NET ランタイム込み(self-contained)のフォルダを作る
  3. 発行結果を検査する(設定ファイルの混入・欠落を防ぐ)
  4. Inno Setup でインストーラーを作る(-SkipInstaller で省略)

  配置の前提(ソリューションのフォルダ):
    <root>\CustomerApp\CustomerApp.vbproj
    <root>\CustomerApp.Tests\CustomerApp.Tests.vbproj
    <root>\Deploy\publish.ps1   ← このファイル
    <root>\Deploy\CustomerApp.iss

.EXAMPLE
  .\Deploy\publish.ps1 -Version 1.0.0
  .\Deploy\publish.ps1 -Version 1.0.1 -SkipTests
#>
[CmdletBinding()]
param(
    [string]$Version = '1.0.0',
    [string]$Runtime = 'win-x64',
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'

$root         = Split-Path -Parent $PSScriptRoot
$appProject   = Join-Path $root 'CustomerApp\CustomerApp.vbproj'
$testProject  = Join-Path $root 'CustomerApp.Tests\CustomerApp.Tests.vbproj'
$publishDir   = Join-Path $root 'artifacts\publish'
$installerDir = Join-Path $root 'artifacts\installer'
$issFile      = Join-Path $PSScriptRoot 'CustomerApp.iss'

function Write-Step([string]$text) {
    Write-Host ''
    Write-Host "=== $text ===" -ForegroundColor Cyan
}

# 外部コマンドは、失敗しても PowerShell が例外にしない。終了コードを自分で確認する。
function Assert-ExitCode([string]$what) {
    if ($LASTEXITCODE -ne 0) {
        throw "$what に失敗しました(終了コード $LASTEXITCODE)。"
    }
}

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version は「1.0.0」の形式で指定してください: $Version"
}
if (-not (Test-Path $appProject)) {
    throw "プロジェクトが見つかりません: $appProject (このスクリプトの置き場所を確認してください)"
}

# ---------- 1. テスト ----------
if (-not $SkipTests) {
    Write-Step '1/4 テスト'
    Write-Host '※ 結合テストは SQL Server(テスト用DB)に接続します。接続できない場合は -SkipTests を付けてください。'
    dotnet test $testProject -c Release
    Assert-ExitCode 'テスト'
} else {
    Write-Step '1/4 テスト(省略)'
}

# ---------- 2. 発行 ----------
Write-Step "2/4 発行 (v$Version, $Runtime, self-contained)"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $appProject `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o $publishDir
Assert-ExitCode '発行'

# ---------- 3. 検査 ----------
Write-Step '3/4 発行結果の検査'

$exe = Join-Path $publishDir 'CustomerApp.exe'
if (-not (Test-Path $exe)) {
    throw "CustomerApp.exe がありません。.vbproj の AssemblyName を確認してください: $exe"
}

$baseSettings = Join-Path $publishDir 'appsettings.json'
if (-not (Test-Path $baseSettings)) {
    throw 'appsettings.json が発行先にありません。ファイルのプロパティ「発行ディレクトリにコピー」を「新しい場合はコピーする」にしてください。'
}

# 開発PCのローカル設定が、配布物に紛れ込んでいないか(紛れると、全員の接続先が開発PCの設定になってしまう)
foreach ($name in 'appsettings.Local.json', 'appsettings.Local.json.example') {
    if (Test-Path (Join-Path $publishDir $name)) {
        throw "$name が発行先に含まれています。「発行ディレクトリにコピー」を「コピーしない」にしてください。"
    }
}

# appsettings.json が正しい JSON か、接続文字列があるか、秘密情報が入っていないか
$json = Get-Content $baseSettings -Raw -Encoding UTF8 | ConvertFrom-Json
$conn = $json.ConnectionStrings.Default
if ([string]::IsNullOrWhiteSpace($conn)) {
    throw 'appsettings.json に ConnectionStrings:Default がありません。'
}
if ($conn -match '(?i)\b(password|pwd)\s*=') {
    throw 'appsettings.json の接続文字列にパスワードが含まれています。配布物に入れてはいけません。'
}

# マイグレーション(SQL)がexeに埋め込まれているか: 簡易確認として、ファイルサイズだけ見る
Write-Host ("exe      : {0:N0} bytes" -f (Get-Item $exe).Length)
Write-Host ("files    : {0}" -f (Get-ChildItem $publishDir -Recurse -File).Count)
Write-Host ("size     : {0:N1} MB" -f ((Get-ChildItem $publishDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB))
Write-Host 'OK'

# ---------- 4. インストーラー ----------
if ($SkipInstaller) {
    Write-Step '4/4 インストーラー(省略)'
    Write-Host "発行フォルダ: $publishDir"
    return
}

Write-Step '4/4 インストーラー'

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw 'Inno Setup 6 (ISCC.exe) が見つかりません。https://jrsoftware.org/isdl.php からインストールしてください。発行フォルダだけ欲しい場合は -SkipInstaller を付けます。'
}

New-Item -ItemType Directory -Path $installerDir -Force | Out-Null

& $iscc "/DMyAppVersion=$Version" "/DSourceDir=$publishDir" "/DOutputDir=$installerDir" $issFile
Assert-ExitCode 'インストーラーの作成'

Write-Host ''
Write-Host "完成: $installerDir" -ForegroundColor Green
Get-ChildItem $installerDir -Filter *.exe | ForEach-Object { Write-Host ("  {0}  ({1:N1} MB)" -f $_.Name, ($_.Length / 1MB)) }
