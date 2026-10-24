Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' BackupServiceデータベースを使わない部分のテスト。
''' </summary>
<TestClass>
Public Class BackupServiceTests

    <TestMethod>
    Public Sub ValidatePath_正しいパスは_Nothing()
        Assert.IsNull(BackupService.ValidatePath("C:\Backup\CustomerDb.bak"))
        Assert.IsNull(BackupService.ValidatePath("  C:\Backup\CustomerDb.BAK  "), "前後の空白と、拡張子の大文字小文字は問わない")
        Assert.IsNull(BackupService.ValidatePath("\\server\share\CustomerDb.bak"), "ネットワークパス")
    End Sub

    <TestMethod>
    Public Sub ValidatePath_空はエラー()
        Assert.IsNotNull(BackupService.ValidatePath(""))
        Assert.IsNotNull(BackupService.ValidatePath("   "))
        Assert.IsNotNull(BackupService.ValidatePath(Nothing))
    End Sub

    <TestMethod>
    Public Sub ValidatePath_拡張子がbakでないとエラー()
        Assert.IsNotNull(BackupService.ValidatePath("C:\Backup\CustomerDb"))
        Assert.IsNotNull(BackupService.ValidatePath("C:\Backup\CustomerDb.mdf"), "データファイルを上書きする事故を防ぐ")
    End Sub

    <TestMethod>
    Public Sub ValidatePath_相対パスはエラー()
        Assert.IsNotNull(BackupService.ValidatePath("CustomerDb.bak"))
        Assert.IsNotNull(BackupService.ValidatePath("..\CustomerDb.bak"))
    End Sub

    <TestMethod>
    Public Sub ValidatePath_使えない文字と長すぎるパスはエラー()
        Assert.IsNotNull(BackupService.ValidatePath("C:\Back*up\CustomerDb.bak"))
        Assert.IsNotNull(BackupService.ValidatePath("C:\Backup\Cust?omer.bak"))
        Assert.IsNotNull(BackupService.ValidatePath("C:\" & New String("a"c, 260) & ".bak"))
    End Sub

    <TestMethod>
    Public Sub QuoteName_角括弧で囲み_閉じ括弧は二重にする()
        Assert.AreEqual("[CustomerDb]", BackupService.QuoteName("CustomerDb"))
        Assert.AreEqual("[a]]b]", BackupService.QuoteName("a]b"))
        Assert.AreEqual("[x]]; DROP DATABASE y; --]", BackupService.QuoteName("x]; DROP DATABASE y; --"), "SQL を差し込まれても、名前の一部になる")
    End Sub

    <TestMethod>
    Public Sub SuggestFileName_データベース名と日時から作る()
        Dim name As String = BackupService.SuggestFileName("CustomerDb", New DateTime(2026, 10, 24, 18, 15, 30))

        Assert.AreEqual("CustomerDb_20261024_181530.bak", name)
    End Sub

    <TestMethod>
    Public Sub SuggestFileName_接尾辞とファイル名に使えない文字()
        Dim name As String = BackupService.SuggestFileName("My:Db", New DateTime(2026, 1, 2, 3, 4, 5), "_before")

        Assert.AreEqual("My_Db_before_20260102_030405.bak", name)
        Assert.IsNull(BackupService.ValidatePath("C:\Backup\" & name))
    End Sub

End Class