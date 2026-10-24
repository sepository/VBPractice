Imports System.IO
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Text

<TestClass>
Public Class CsvExporterTests

    Private ReadOnly _tempFiles As New List(Of String)()

    <TestCleanup>
    Public Sub CleanUp()
        For Each f As String In _tempFiles
            If File.Exists(f) Then
                File.Delete(f)
            End If
        Next
        _tempFiles.Clear()
    End Sub

#Region "テスト用の道具"

    Private Function NewTempPath() As String
        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"customerapp-test-{Guid.NewGuid():N}.csv")
        _tempFiles.Add(path)
        Return path
    End Function

    Private Shared Function SingleColumn(value As Object) As ExportTable
        Dim table As New ExportTable()
        table.Headers.Add("値")
        table.Rows.Add(New Object() {value})
        Return table
    End Function

    Private Function WriteAndReadLines(table As ExportTable) As String()
        Dim path As String = NewTempPath()
        CsvExporter.Save(table, path)

        Dim text As String = File.ReadAllText(path, Encoding.UTF8)
        Return text.Split({vbCrLf}, StringSplitOptions.None)
    End Function

#End Region

    <TestMethod>
    Public Sub Save_ファイルの先頭にBOMが付く()
        Dim path As String = NewTempPath()

        CsvExporter.Save(SingleColumn("a"), path)

        ' UTF-8 の BOM は EF BB BF(10進数で 239, 187, 191)。無いと、日本語版 Excel で文字化けする
        Dim bytes As Byte() = File.ReadAllBytes(path)
        Assert.IsGreaterThanOrEqualTo(3, bytes.Length)
        Assert.AreEqual(239, CInt(bytes(0)))
        Assert.AreEqual(187, CInt(bytes(1)))
        Assert.AreEqual(191, CInt(bytes(2)))
    End Sub

    <TestMethod>
    Public Sub Save_見出し行とデータ行が出力される()
        Dim table As New ExportTable()
        table.Headers.AddRange(New String() {"ID", "名前"})
        table.Rows.Add(New Object() {1, "山田 太郎"})
        table.Rows.Add(New Object() {2, "佐藤 花子"})

        Dim lines As String() = WriteAndReadLines(table)

        Assert.AreEqual("ID,名前", lines(0))
        Assert.AreEqual("1,山田 太郎", lines(1))
        Assert.AreEqual("2,佐藤 花子", lines(2))
    End Sub

    <TestMethod>
    Public Sub Save_行の区切りはCRLF()
        Dim table As New ExportTable()
        table.Headers.Add("値")
        table.Rows.Add(New Object() {"a"})
        Dim path As String = NewTempPath()

        CsvExporter.Save(table, path)

        Dim text As String = File.ReadAllText(path, Encoding.UTF8)
        Assert.AreEqual("値" & vbCrLf & "a" & vbCrLf, text)
    End Sub

    <TestMethod>
    Public Sub Save_カンマを含む値はダブルクォートで囲まれる()
        Dim q As String = ChrW(34)
        Dim lines As String() = WriteAndReadLines(SingleColumn("東京都,千代田区"))

        Assert.AreEqual(q & "東京都,千代田区" & q, lines(1))
    End Sub

    <TestMethod>
    Public Sub Save_ダブルクォートは2つ重ねてエスケープされる()
        Dim q As String = ChrW(34)
        Dim input As String = "say " & q & "hi" & q                     ' 入力 : say "hi"
        Dim lines As String() = WriteAndReadLines(SingleColumn(input))
        Dim expected As String = q & "say " & q & q & "hi" & q & q & q      ' 期待 : "say ""hi"""

        Assert.AreEqual(expected, lines(1))
    End Sub

    <TestMethod>
    Public Sub Save_改行を含む値はダブルクォートで囲まれる()
        Dim q As String = ChrW(34)
        Dim table As New ExportTable()
        table.Headers.Add("金額,税込")
        table.Rows.Add(New Object() {100})
        Dim lines As String() = WriteAndReadLines(table)

        Assert.AreEqual(q & "金額,税込" & q, lines(0))
    End Sub

    ' ---- CSVインジェクション対策 ----

    <TestMethod>
    <DataRow("=1+1")>
    <DataRow("+81-90-0000-0000")>
    <DataRow("-cmd")>
    <DataRow("@SUM")>
    Public Sub Save_数式として実行されうる文字列は先頭に引用符が付く(input As String)
        Dim lines As String() = WriteAndReadLines(SingleColumn(input))

        Assert.AreEqual("'" & input, lines(1))
    End Sub

    <TestMethod>
    Public Sub Save_数値型のマイナスは引用符を付けない()
        Dim lines As String() = WriteAndReadLines(SingleColumn(-100))

        Assert.AreEqual("-100", lines(1))
    End Sub

    ' ---- 型ごとの書き方 ----

    <TestMethod>
    Public Sub Save_Decimalは桁区切りなしで小数点以下の無駄な0を付けない()
        Assert.AreEqual("1200", WriteAndReadLines(SingleColumn(1200D))(1))
        Assert.AreEqual("1234567.5", WriteAndReadLines(SingleColumn(1234567.5D))(1))
        Assert.AreEqual("0.1", WriteAndReadLines(SingleColumn(0.1D))(1))
    End Sub

    <TestMethod>
    Public Sub Save_日付はyyyy_MM_dd形式()
        Dim lines As String() = WriteAndReadLines(SingleColumn(New DateTime(2026, 10, 1, 13, 45, 0)))

        Assert.AreEqual("2026/10/01", lines(1))
    End Sub

    <TestMethod>
    Public Sub Save_NothingとDBNullは空欄になる()
        Dim table As New ExportTable()
        table.Headers.AddRange(New String() {"A", "B", "C"})
        table.Rows.Add(New Object() {Nothing, DBNull.Value, "x"})
        Dim lines As String() = WriteAndReadLines(table)

        Assert.AreEqual(",,x", lines(1))
    End Sub

    <TestMethod>
    Public Sub Save_データが0行でも見出しだけは出力される()
        Dim table As New ExportTable()
        table.Headers.AddRange(New String() {"A", "B"})
        Dim lines As String() = WriteAndReadLines(table)

        Assert.AreEqual("A,B", lines(0))
        Assert.AreEqual(String.Empty, lines(1))
    End Sub

End Class
