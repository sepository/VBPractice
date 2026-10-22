Imports System.IO
Imports Microsoft.Extensions.Configuration


''' <summary>
''' アプリ全体の設定。まずは定数で持ち、慣れてきたら appsettings.json などに移す。
''' </summary>
Public Module AppConfig

    Private Const BaseFileName As String = "appsettings.json"
    Private Const LocalFileName As String = "appsettings.local.json"
    Private Const EnvironmentPrefix As String = "CUSTOMERAPP_"

    Private ReadOnly SyncRoot As New Object()
    Private _configuration As IConfigurationRoot

    ''' <summary>
    ''' 設定の読み込み元を組み立てる。
    ''' </summary>
    ''' <param name="basePath">
    ''' 設定ファイルのあるフォルダ
    ''' </param>
    ''' <param name="environmentPrefix">
    ''' 読み込む環境変数の接頭辞
    ''' </param>
    ''' <returns></returns>
    Public Function CreateConfiguration(basePath As String, environmentPrefix As String) As IConfigurationRoot
        ' AddJsonFileの引数は(ファイル名, 無くてもよいか, ファイルの変更を監視して再読み込みするか)
        ' appsettings.json          : 無くてはならない
        ' appsettings.local.json    : 無くてもよい
        Dim builder As New ConfigurationBuilder()
        builder.SetBasePath(basePath)
        builder.AddJsonFile(BaseFileName, False, False)

        ' ローカル設定ファイルが「空(0バイト・空白)」の場合は、読み込まない。
        ' 空のファイルはJSONとして正しくないため、エラーで起動できなくなる。
        If Not IsBlankFile(Path.Combine(basePath, LocalFileName)) Then
            builder.AddJsonFile(LocalFileName, True, False)
        End If

        builder.AddEnvironmentVariables(environmentPrefix)

        Return builder.Build()
    End Function

    ''' <summary>
    ''' ファイルがない、もしくは中身が空
    ''' </summary>
    Private Function IsBlankFile(filePath As String) As Boolean
        If Not File.Exists(filePath) Then
            Return True
        End If

        Return String.IsNullOrWhiteSpace(File.ReadAllText(filePath))
    End Function

    ''' <summary>
    ''' アプリ全体で共有する設定。一度だけ読み込む。
    ''' </summary>
    ''' <returns></returns>
    Private Function Configuration() As IConfigurationRoot
        SyncLock SyncRoot
            If _configuration Is Nothing Then
                ' AppContext.BaseDirectory = 実行ファイルがあるフォルダ
                _configuration = CreateConfiguration(AppContext.BaseDirectory, EnvironmentPrefix)
            End If

            Return _configuration
        End SyncLock
    End Function

    ''' <summary>
    ''' 設定を読み込めるか、必須の設定があるかを確認する。
    ''' </summary>
    Public Sub Validate()
        LoadConnectionString()
    End Sub

    ''' <summary>
    ''' ローカル設定ファイルが存在するか。
    ''' </summary>
    Public Function HasLocalSettings() As Boolean
        Return File.Exists(Path.Combine(AppContext.BaseDirectory, LocalFileName))
    End Function

    ''' <summary>
    ''' データベースの接続文字列
    ''' ConnectionStrings:Defaultを取得する。
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ConnectionString As String
        Get
            Return LoadConnectionString()
        End Get
    End Property

    Private Function LoadConnectionString() As String
        Dim config As IConfigurationRoot = Configuration()
        Dim value As String = config.GetConnectionString("Default")

        If String.IsNullOrWhiteSpace(value) Then
            Throw New InvalidOperationException(
                "接続文字列が設定されていません。appsettings.jsonのConnectionStrings:Defaultを確認してください。")
        End If

        Return value
    End Function

    ''' <summary>
    ''' 任意の文字列を読み込む。
    ''' 設定がなければ、defualtValueを返す。
    ''' </summary>
    Public Function GetString(key As String, defaultValue As String) As String
        Dim config As IConfigurationRoot = Configuration()
        Dim value As String = config(key)

        If String.IsNullOrWhiteSpace(value) Then
            Return defaultValue
        End If

        Return value
    End Function

    ''' <summary>
    ''' 任意の整数を読み込む。
    ''' 設定がなければ、defualtValueを返す。
    ''' </summary>
    Public Function GetInt(key As String, defaultValue As Integer) As Integer
        Dim parsed As Integer
        If Integer.TryParse(GetString(key, Nothing), parsed) Then
            Return parsed
        End If

        Return defaultValue
    End Function

End Module
