Imports System.Text.RegularExpressions
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class SqlBatchSplitterTests

    <TestMethod>
    Public Sub Split_GOの行でバッチに分かれる()
        Dim batches As List(Of String) = SqlBatchSplitter.Split("SELECT 1;" & vbCrLf & "GO" & vbCrLf & "SELECT 2;")

        Assert.HasCount(2, batches)
        Assert.AreEqual("SELECT 1;", batches(0))
        Assert.AreEqual("SELECT 2;", batches(1))
    End Sub

    <TestMethod>
    Public Sub Split_GOは大文字小文字を問わず_前後の空白があっても区切りになる()
        Dim batches As List(Of String) = SqlBatchSplitter.Split("SELECT 1;" & vbCrLf & "   go   " & vbCrLf & "SELECT 2;")

        Assert.HasCount(2, batches)
    End Sub

    <TestMethod>
    Public Sub Split_GOを含むだけの行は区切りにならない()
        ' GOTO, 'GO'などを区切りと間違えてはいけない。
        Dim script As String = "SELECT 'GO';" & vbCrLf & "GOTO label;" & vbCrLf & "SELECT 2;"
        Dim batches As List(Of String) = SqlBatchSplitter.Split(script)

        Assert.HasCount(1, batches)
    End Sub

    <TestMethod>
    Public Sub Split_空のバッチは捨てられる()
        Dim batches As List(Of String) = SqlBatchSplitter.Split("GO" & vbCrLf & vbCrLf & "GO" & vbCrLf & "SELECT 1;" & vbCrLf & "GO" & vbCrLf)

        Assert.HasCount(1, batches)
        Assert.AreEqual("SELECT 1;", batches(0))
    End Sub

    <TestMethod>
    Public Sub Split_GOがなければ1つのバッチになる()
        Dim batches As List(Of String) = SqlBatchSplitter.Split("SELECT 1;" & vbCrLf & "SELECT 2;")

        Assert.HasCount(1, batches)
    End Sub

    <TestMethod>
    Public Sub Split_改行がCRLFでも分けられる()
        Dim batches As List(Of String) = SqlBatchSplitter.Split("SELECT 1;" & vbCrLf & "GO" & vbCrLf & "SELECT 2;")

        Assert.HasCount(2, batches)
        Assert.AreEqual("SELECT 1;", batches(0))
    End Sub

    <TestMethod>
    Public Sub Split_バッチの中の複数行と順序は保たれる()
        Dim script As String = "IF 1 = 1" & vbLf & "BEGIN" & vbLf & "    SELECT 1;" & vbLf & "END" & vbLf & "GO" & vbLf & "SELECT 2;"
        Dim batches As List(Of String) = SqlBatchSplitter.Split(script)

        Assert.HasCount(2, batches)
        Assert.StartsWith("IF 1 = 1", batches(0))
        Assert.Contains("SELECT 1;", batches(0))
        Assert.EndsWith("END", batches(0))
    End Sub

End Class

<TestClass>
Public Class MigrationChecksumTests

    <TestMethod>
    Public Sub Compute_同じ内容なら同じ値になる()
        Assert.AreEqual(MigrationChecksum.Compute("SELECT 1;"), MigrationChecksum.Compute("SELECT 1;"))
    End Sub

    <TestMethod>
    Public Sub Compute_内容が違えば違う値になる()
        Assert.AreNotEqual(MigrationChecksum.Compute("SELECT 1;"), MigrationChecksum.Compute("SELECT 2;"))
    End Sub

    <TestMethod>
    Public Sub Compute_改行コードが違うだけなら同じ値になる()
        Dim withLf As String = "SELECT 1;" & vbLf & "SELECT 2;"
        Dim withCrLf As String = "SELECT 1;" & vbCrLf & "SELECT 2;"

        Assert.AreEqual(MigrationChecksum.Compute(withLf), MigrationChecksum.Compute(withCrLf))
    End Sub

    <TestMethod>
    Public Sub Compute_先頭のBOMと前後の空白は無視される()
        Dim plain As String = "SELECT 1;"
        Dim decorated As String = ChrW(&HFEFF) & vbLf & plain & vbLf & vbLf

        Assert.AreEqual(MigrationChecksum.Compute(plain), MigrationChecksum.Compute(decorated))
    End Sub

    <TestMethod>
    Public Sub Compute_16進数64文字になる()
        Dim checkSum As String = MigrationChecksum.Compute("SELECT 1;")

        Assert.AreEqual(64, checkSum.Length)
        Assert.IsTrue(Regex.IsMatch(checkSum, "^[0-9A-F]{64}$"))
    End Sub

End Class

<TestClass>
Public Class EmbeddedMigrationsTests

    <TestMethod>
    Public Sub Load_埋め込まれたスクリプトを読み込める()
        Dim scripts As List(Of MigrationScript) = EmbeddedMigrations.Load()

        Assert.IsGreaterThanOrEqualTo(4, scripts.Count, "V001~V004が読み込まれるはず")
    End Sub

    <TestMethod>
    Public Sub Load_番号は1から欠番なしの連番で並ぶ()
        Dim scripts As List(Of MigrationScript) = EmbeddedMigrations.Load()

        For i As Integer = 0 To scripts.Count - 1
            Assert.AreEqual(i + 1, scripts(i).Version, $"{i + 1}番目のスクリプトの番号が連番になっていない: {scripts(i).Name}")
        Next
    End Sub

    <TestMethod>
    Public Sub Load_名前は規則に合い_中身とチェックサムがある()
        For Each script As MigrationScript In EmbeddedMigrations.Load()
            Assert.IsTrue(Regex.IsMatch(script.Name, "^V\d{3}__.+$"), $"名前が規則に合っていない: {script.Name}")
            Assert.IsFalse(String.IsNullOrWhiteSpace(script.Sql), $"中身が空: {script.Name}")
            Assert.AreEqual(64, script.Checksum.Length)
        Next
    End Sub

    <TestMethod>
    Public Sub Load_スクリプトにデータベース名の指定やサンプルデータが含まれていない()
        ' マイグレーションは、どのデータベース名でも動き、本番に不要なデータを入れてはいけない
        For Each script As MigrationScript In EmbeddedMigrations.Load()
            Dim code As String = Regex.Replace(script.Sql, "/\*.*?\*/", String.Empty, RegexOptions.Singleline)
            code = Regex.Replace(code, "--.*$", String.Empty, RegexOptions.Multiline)
            Dim sql As String = code.ToUpperInvariant()

            Assert.IsFalse(Regex.IsMatch(sql, "^\s*USE\s+", RegexOptions.Multiline), $"USE が含まれている: {script.Name}")
            Assert.DoesNotContain("CREATE DATABASE", sql, $"CREATE DATABASE が含まれている: {script.Name}")
            Assert.DoesNotContain("山田 太郎", code, $"サンプルデータが含まれている: {script.Name}")
        Next
    End Sub

End Class