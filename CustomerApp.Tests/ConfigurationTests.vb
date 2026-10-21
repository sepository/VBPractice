Imports System.IO
Imports Microsoft.Extensions.Configuration
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' 設定ファイルの読み込み
''' </summary>
<TestClass>
Public Class ConfigurationTests

    Private _tempDirectory As String
    Private _environmentPrefix As String
    Private ReadOnly _environmentVariables As New List(Of String)()

    <TestInitialize>
    Public Sub Setup()
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"customerapp-config-{Guid.NewGuid():N}")
        Directory.CreateDirectory(_tempDirectory)

        ' このテスト専用の接頭辞
        _environmentPrefix = $"CUSTOMERAPPTEST{Guid.NewGuid():N}_"
    End Sub

    <TestCleanup>
    Public Sub Cleanup()
        For Each name As String In _environmentVariables
            ' Nothingを設定すると、環境変数が削除される
            Environment.SetEnvironmentVariable(name, Nothing)
        Next
        _environmentVariables.Clear()

        If Directory.Exists(_tempDirectory) Then
            Directory.Delete(_tempDirectory, True)
        End If
    End Sub

    ' ---- テスト用の道具 ----

    Private Sub WriteSettingsFile(fileName As String, json As String)
        File.WriteAllText(Path.Combine(_tempDirectory, fileName), json)
    End Sub

    Private Sub SetEnvironmentVariable(nameWithoutPrefix As String, value As String)
        Dim name As String = _environmentPrefix & nameWithoutPrefix
        Environment.SetEnvironmentVariable(name, value)
        _environmentVariables.Add(name)
    End Sub

    Private Function Build() As IConfigurationRoot
        Return AppConfig.CreateConfiguration(_tempDirectory, _environmentPrefix)
    End Function

    ' ---- テスト ----

    <TestMethod>
    Public Sub CreateConfiguration_設定ファイルから接続文字列を読める()
        WriteSettingsFile("appsettings.json", "{ ""ConnectionStrings"": { ""Default"": ""FROM-JSON"" } }")

        Dim config As IConfigurationRoot = Build()

        Assert.AreEqual("FROM-JSON", config.GetConnectionString("Default"))
    End Sub

    <TestMethod>
    Public Sub CreateConfiguration_JSONの二重バックスラッシュは1つのバックスラッシュとして読まれる()
        ' JSONでは、バックスラッシュを「\\」と書く決まり。
        WriteSettingsFile("appsettings.json", "{ ""ConnectionStrings"": { ""Default"": ""Server=localhost\\SQLEXPRESS;Database=CustomerDb"" } }")

        Dim config As IConfigurationRoot = Build()

        Assert.AreEqual("Server=localhost\SQLEXPRESS;Database=CustomerDb", config.GetConnectionString("Default"))
    End Sub

    <TestMethod>
    Public Sub CreateConfiguration_ローカル設定ファイルが基本の設定を上書きする()
        WriteSettingsFile("appsettings.json", "{ ""ConnectionStrings"": { ""Default"": ""BASE"" }, ""Logging"": { ""RetentionDays"": 30 } }")
        WriteSettingsFile("appsettings.local.json", "{ ""ConnectionStrings"": { ""Default"": ""LOCAL"" } }")

        Dim config As IConfigurationRoot = Build()

        Assert.AreEqual("LOCAL", config.GetConnectionString("Default"), "ローカル設定が優先されるはず")
        Assert.AreEqual("30", config("Logging:RetentionDays"), "ローカル設定にない項目は、基本設定のまま残るはず")
    End Sub

    <TestMethod>
    Public Sub CreateConfiguration_環境変数はファイルより優先される()
        WriteSettingsFile("appsettings.json", "{ ""ConnectionStrings"": { ""Default"": ""FROM-JSON"" } }")
        WriteSettingsFile("appsettings.local.json", "{ ""ConnectionStrings"": { ""Default"": ""FROM-LOCAL"" } }")

        ' 階層の区切りは「__」(アンダースコア2つ)
        SetEnvironmentVariable("ConnectionStrings__Default", "FROM-ENV")

        Dim config As IConfigurationRoot = Build()

        Assert.AreEqual("FROM-ENV", config.GetConnectionString("Default"))
    End Sub

    <TestMethod>
    Public Sub CreateConfiguration_基本の設定ファイルが無ければ例外になる()
        Dim caught As Exception = Nothing
        Try
            Build()
        Catch ex As Exception
            caught = ex
        End Try

        Assert.IsNotNull(caught, "例外が起きるはず")
        Assert.IsInstanceOfType(Of FileNotFoundException)(caught)
    End Sub

    <TestMethod>
    Public Sub CreateConfiguration_ローカル設定ファイルは無くてもよい()
        WriteSettingsFile("appsettings.json", "{ ""ConnectionStrings"": { ""Default"": ""BASE"" } }")

        Dim config As IConfigurationRoot = Build()

        Assert.AreEqual("BASE", config.GetConnectionString("Default"))
    End Sub

    <TestMethod>
    Public Sub CreateConfiguration_接続文字列が無ければNothingが返る()
        WriteSettingsFile("appsettings.json", "{ }")

        Dim config As IConfigurationRoot = Build()

        Assert.IsNull(config.GetConnectionString("Default"))
    End Sub

End Class
