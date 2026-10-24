Imports System.Numerics
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' CSVの読込テスト
''' </summary>
<TestClass>
Public Class CsvParserTests

    ' 「"」を読みやすく書くための定数
    Private Const Q As String = """"

    Private Shared Function Join(ParamArray lines As String()) As String
        Return String.Join(vbCrLf, lines)
    End Function

    <TestMethod>
    Public Sub Parse_行と項目に分かれる()
        Dim records As List(Of CsvRecord) = CsvParser.Parse(Join("a,b,c", "1,2,3"))

        Assert.HasCount(2, records)
        CollectionAssert.AreEqual(New String() {"a", "b", "c"}, records(0).Fields)
        CollectionAssert.AreEqual(New String() {"1", "2", "3"}, records(1).Fields)
    End Sub

    <TestMethod>
    Public Sub Parse_最後に改行があってもなくても同じ結果になる()
        Dim without As List(Of CsvRecord) = CsvParser.Parse("a,b" & vbLf & "1,2")
        Dim withNewLine As List(Of CsvRecord) = CsvParser.Parse("a,b" & vbLf & "1,2" & vbLf)

        Assert.HasCount(2, without)
        Assert.HasCount(2, withNewLine)
    End Sub

    <TestMethod>
    Public Sub Parse_改行はCRLF_LF_CRのどれでもよい()
        Assert.HasCount(3, CsvParser.Parse("a" & vbCrLf & "b" & vbCrLf & "c"))
        Assert.HasCount(3, CsvParser.Parse("a" & vbCr & "b" & vbCr & "c"))
        Assert.HasCount(3, CsvParser.Parse("a" & vbLf & "b" & vbLf & "c"))
    End Sub

    <TestMethod>
    Public Sub Parse_引用符の中のカンマは区切りにならない()
        Dim records As List(Of CsvRecord) = CsvParser.Parse("名前," & Q & "東京都千代田区1-1, ビル3階" & Q)

        Assert.HasCount(2, records(0).Fields)
        Assert.AreEqual("東京都千代田区1-1, ビル3階", records(0).Fields(1))
    End Sub

    <TestMethod>
    Public Sub Parse_引用符の中の改行は値の一部になり_行番号は物理行を数える()
        Dim records As List(Of CsvRecord) = CsvParser.Parse("a,b" & vbLf & "1," & Q & "x" & vbLf & "y" & Q & vbLf & "3,4")

        Assert.HasCount(3, records)
        Assert.AreEqual("x" & vbLf & "y", records(1).Fields(1))
        Assert.AreEqual(1, records(0).LineNumber)
        Assert.AreEqual(2, records(1).LineNumber)
        Assert.AreEqual(4, records(2).LineNumber, "引用符の中の改行も行数に数える")
    End Sub

    <TestMethod>
    Public Sub Parse_二重の引用符は引用符1文字になる()
        Dim records As List(Of CsvRecord) = CsvParser.Parse(Q & "彼は" & Q & Q & "OK" & Q & Q & "と言った" & Q)

        Assert.AreEqual("彼は" & Q & "OK" & Q & "と言った", records(0).Fields(0))
    End Sub

    <TestMethod>
    Public Sub Parse_空の項目は空文字になる()
        Dim records As List(Of CsvRecord) = CsvParser.Parse("a,,c,")

        Assert.HasCount(4, records(0).Fields)
        Assert.AreEqual(String.Empty, records(0).Fields(1))
        Assert.AreEqual(String.Empty, records(0).Fields(3))
    End Sub

    <TestMethod>
    Public Sub Parse_空行は読み飛ばされ_行番号はずれない()
        Dim records As List(Of CsvRecord) = CsvParser.Parse("a" & vbLf & vbLf & vbLf & "b")

        Assert.HasCount(2, records)
        Assert.AreEqual(4, records(1).LineNumber)
    End Sub

    <TestMethod>
    Public Sub Parse_先頭のBOMは無視される()
        Dim records As List(Of CsvRecord) = CsvParser.Parse(ChrW(&HFEFF) & "名前,メール")

        Assert.AreEqual("名前", records(0).Fields(0))
    End Sub

    <TestMethod>
    Public Sub Parse_Nothingや空文字はレコードなしになる()
        Assert.IsEmpty(CsvParser.Parse(Nothing))
        Assert.IsEmpty(CsvParser.Parse(String.Empty))
    End Sub

    <TestMethod>
    Public Sub Parse_引用は閉じていなければ_始まった行を示す例外になる()
        Dim caught As CsvFormatException = CatchFormatException("a,b" & vbLf & "1," & Q & "閉じていない" & vbLf & "3,4")

        Assert.IsNotNull(caught, "例外が起きるはず")
        Assert.AreEqual(2, caught.LineNumber)
    End Sub

    <TestMethod>
    Public Sub Parse_引用符で囲まない項目の途中の引用符は例外になる()
        Assert.IsNotNull(CatchFormatException("ab" & Q & "c,d"))
    End Sub

    <TestMethod>
    Public Sub Parse_閉じた引用符の後に余計な文字があれば例外になる()
        Assert.IsNotNull(CatchFormatException(Q & "ab" & Q & "c,d"))
    End Sub

    Private Shared Function CatchFormatException(text As String) As CsvFormatException
        Try
            CsvParser.Parse(text)
        Catch ex As CsvFormatException
            Return ex
        End Try

        Return Nothing
    End Function

End Class

''' <summary>
''' 文字コードの自動判定
''' </summary>
<TestClass>
Public Class CsvFileReaderTests

    Private Const Sample As String = "名前,住所" & vbLf & "山田 太郎,東京都"

    <TestMethod>
    Public Sub Decode_BOMつきUTF8を判定できる()
        Dim bytes As Byte() = New UTF8Encoding(True).GetPreamble().Concat(Encoding.UTF8.GetBytes(Sample)).ToArray()
        Dim name As String = Nothing

        Assert.AreEqual(Sample, CsvFileReader.Decode(bytes, name))
        Assert.AreEqual("UTF-8 (BOMあり)", name)
    End Sub

    <TestMethod>
    Public Sub Decode_BOMなしUTF8を判定できる()
        Dim name As String = Nothing

        Assert.AreEqual(Sample, CsvFileReader.Decode(Encoding.UTF8.GetBytes(Sample), name))
        Assert.AreEqual("UTF-8", name)
    End Sub

    <TestMethod>
    Public Sub Decode_ShiftJISを判定できる()
        ' Excel で「CSV」として保存すると、日本語版では Shift_JIS(CP932)になる
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        Dim bytes As Byte() = Encoding.GetEncoding(932).GetBytes(Sample)
        Dim name As String = Nothing

        Assert.AreEqual(Sample, CsvFileReader.Decode(bytes, name))
        Assert.AreEqual("Shift_JIS (CP932)", name)
    End Sub

    <TestMethod>
    Public Sub Decode_UTF16LEのBOMを判定できる()
        Dim bytes As Byte() = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(Sample)).ToArray()
        Dim name As String = Nothing

        Assert.AreEqual(Sample, CsvFileReader.Decode(bytes, name))
        Assert.AreEqual("UTF-16 LE", name)
    End Sub

    <TestMethod>
    Public Sub Decode_英数字だけでも読める()
        Dim name As String = Nothing

        Assert.AreEqual("name,email", CsvFileReader.Decode(Encoding.ASCII.GetBytes("name,email"), name))
    End Sub

    <TestMethod>
    Public Sub Decode_空のファイルは空文字になる()
        Dim name As String = Nothing

        Assert.AreEqual(String.Empty, CsvFileReader.Decode(New Byte() {}, name))
    End Sub

End Class

''' <summary>
''' 顧客のCSVの検証
''' </summary>
<TestClass>
Public Class CustomerCsvImporterTests

    Private Const Header As String = "名前,フリガナ,メール,電話,住所,メモ"

    Private Shared Function Csv(ParamArray lines As String()) As String
        Return String.Join(vbCrLf, lines)
    End Function

    Private Shared Function Analyze(csvText As String, ParamArray existingEmails As String()) As CustomerImportPlan
        Return CustomerCsvImporter.Analyze(csvText, existingEmails)
    End Function

    Private Shared Sub AssertContains(text As String, part As String)
        Assert.IsGreaterThanOrEqualTo(0, text.IndexOf(part, StringComparison.Ordinal), $"「{part}」が含まれるはず: {text}")
    End Sub

    ' ---- 正常 ----

    <TestMethod>
    Public Sub Analyze_正しいファイルは取り込める()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header,
            "山田 太郎,ヤマダ タロウ,taro@example.com,03-1234-5678,東京都,メモ1",
            "佐藤 花子,,hanako@example.com,,,"))

        Assert.IsEmpty(plan.Errors)
        Assert.IsEmpty(plan.Skipped)
        Assert.HasCount(2, plan.Customers)
        Assert.AreEqual(2, plan.DataRowCount)
        Assert.IsTrue(plan.CanImport)

        Assert.AreEqual("山田 太郎", plan.Customers(0).Name)
        Assert.AreEqual("ヤマダ タロウ", plan.Customers(0).Kana)
        Assert.AreEqual("taro@example.com", plan.Customers(0).Email)
        Assert.AreEqual("東京都", plan.Customers(0).Address)
        Assert.AreEqual(String.Empty, plan.Customers(1).Kana)
    End Sub

    <TestMethod>
    Public Sub Analize_列の番号は自由で必須は名前だけ()
        Dim plan As CustomerImportPlan = Analyze(Csv("メール,名前", "a@example.com,山田"))

        Assert.IsEmpty(plan.Errors)
        Assert.AreEqual("山田", plan.Customers(0).Name)
        Assert.AreEqual("a@example.com", plan.Customers(0).Email)
    End Sub

    <TestMethod>
    Public Sub Analyze_英語の見出しも使える()
        Dim plan As CustomerImportPlan = Analyze(Csv("Name,Email", "Taro,t@example.com"))

        Assert.IsEmpty(plan.Errors)
        Assert.AreEqual("Taro", plan.Customers(0).Name)
    End Sub

    <TestMethod>
    Public Sub Analyze_前後の空白は取り除かれる()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, "  山田 太郎  ,,  a@example.com ,,,"))

        Assert.AreEqual("山田 太郎", plan.Customers(0).Name)
        Assert.AreEqual("a@example.com", plan.Customers(0).Email)
    End Sub

    <TestMethod>
    Public Sub Analyze_エクスポートした顧客一覧のCSVをそのまま取り込める()
        Dim source As New Customer With {
            .CustomerId = 5, .Name = "山田 太郎", .Email = "taro@example.com",
            .Address = "東京都千代田区1-1, ビル3階", .Memo = "=SUM(1)", .OrderCount = 3, .TotalAmount = 1500D
        }

        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"customerapp-roundtrip-{Guid.NewGuid():N}.csv")
        Try
            CsvExporter.Save(ExportTableBuilder.FromCustomers(New Customer() {source}), path)

            Dim encodingName As String = Nothing
            Dim plan As CustomerImportPlan = Analyze(CsvFileReader.ReadFile(path, encodingName))

            Assert.IsEmpty(plan.Errors)
            Assert.HasCount(1, plan.Customers)
            Assert.AreEqual("山田 太郎", plan.Customers(0).Name)
            Assert.AreEqual("東京都千代田区1-1, ビル3階", plan.Customers(0).Address, "カンマを含む項目も戻る")
            Assert.AreEqual("=SUM(1)", plan.Customers(0).Memo, "出力時に付けた ' は取り除かれる")
        Finally
            If System.IO.File.Exists(path) Then
                System.IO.File.Delete(path)
            End If
        End Try
    End Sub

    <TestMethod>
    Public Sub Analyze_数式の保護の印は数式の形のときだけ取り除く()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header,
           "A,,,,,'+81 90",
           "B,,,,,'abc"))

        Assert.AreEqual("+81 90", plan.Customers(0).Memo)
        Assert.AreEqual("'abc", plan.Customers(1).Memo, "= + - @ が続かない ' は、本物の文字なので残す")
    End Sub

    ' ---- エラー ----

    <TestMethod>
    Public Sub Analyze_名前が空なら行番号つきのエラーになる()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, "山田,,,,,", ",,a@example.com,,,"))

        Assert.HasCount(1, plan.Errors)
        Assert.AreEqual(3, plan.Errors(0).LineNumber)
        AssertContains(plan.Errors(0).Message, "名前")
        Assert.IsFalse(plan.CanImport)
    End Sub

    <TestMethod>
    Public Sub Analyze_エラーは最初の1件で止まらず全部報告される()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header,
            ",,,,,",
            "OK,,,,,",
            "山田,,not-an-email,,,",
            "X,,,,,,余分"))

        Assert.HasCount(3, plan.Errors)
        Assert.AreEqual(2, plan.Errors(0).LineNumber)
        Assert.AreEqual(4, plan.Errors(1).LineNumber)
        Assert.AreEqual(5, plan.Errors(2).LineNumber)
    End Sub

    <TestMethod>
    Public Sub Analyze_文字数の上限を超えるとエラーになる()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, New String("あ"c, CustomerCsvImporter.MaxNameLength + 1) & ",,,,,"))

        Assert.HasCount(1, plan.Errors)
        AssertContains(plan.Errors(0).Message, "長すぎ")
    End Sub

    <TestMethod>
    Public Sub Analyze_上限ちょうどの文字数は取り込める()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, New String("あ"c, CustomerCsvImporter.MaxNameLength) & ",,,,,"))

        Assert.IsEmpty(plan.Errors)
        Assert.HasCount(1, plan.Customers)
    End Sub

    <TestMethod>
    Public Sub Analyze_メールアドレスの形式が正しくなければエラーになる()
        For Each bad As String In New String() {"abc", "@example.com", "a@", "a@@b.com", "a b@example.com"}
            Dim plan As CustomerImportPlan = Analyze(Csv(Header, $"山田,,{bad},,,"))

            Assert.HasCount(1, plan.Errors, $"エラーになるはず: {bad}")
        Next
    End Sub

    <TestMethod>
    Public Sub Analyze_項目の数がヘッダーと違う()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, "山田,,,", "佐藤,,,,,"))

        Assert.HasCount(1, plan.Errors)
        Assert.AreEqual(2, plan.Errors(0).LineNumber)
        AssertContains(plan.Errors(0).Message, "項目の数")
    End Sub

    <TestMethod>
    Public Sub Analyze_ヘッダーに名前がなければエラーになる()
        Dim plan As CustomerImportPlan = Analyze(Csv("メール,電話", "a@example.com,1"))

        Assert.HasCount(1, plan.Errors)
        Assert.AreEqual(1, plan.Errors(0).LineNumber)
        Assert.IsEmpty(plan.Customers)
    End Sub

    <TestMethod>
    Public Sub Analyze_ヘッダーに同じ項目が2回あればエラーになる()
        Dim plan As CustomerImportPlan = Analyze(Csv("名前,名前", "a,b"))

        Assert.HasCount(1, plan.Errors)
    End Sub

    <TestMethod>
    Public Sub Analyze_空のファイルやヘッダーだけのファイルはエラーになる()
        Assert.HasCount(1, Analyze(String.Empty).Errors)
        Assert.HasCount(1, Analyze(Header).Errors)
    End Sub

    <TestMethod>
    Public Sub Analyze_CSVの書式が壊れていればその行はエラーになる()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, "山田,,,,,""閉じていない"))

        Assert.HasCount(1, plan.Errors)
        Assert.AreEqual(2, plan.Errors(0).LineNumber)
        Assert.IsFalse(plan.CanImport)
    End Sub

    <TestMethod>
    Public Sub Analyze_行数の上限を超えるとエラーになる()
        Dim sb As New StringBuilder()
        sb.AppendLine("名前")
        For i As Integer = 1 To CustomerCsvImporter.MaxRows + 1
            sb.AppendLine($"顧客{i}")
        Next

        Dim plan As CustomerImportPlan = Analyze(sb.ToString())

        Assert.HasCount(1, plan.Errors)
        Assert.IsEmpty(plan.Customers)
    End Sub

    <TestMethod>
    Public Sub Analyze_行数の上限ちょうどは取り込める()
        Dim sb As New StringBuilder()
        sb.AppendLine("名前")
        For i As Integer = 1 To CustomerCsvImporter.MaxRows
            sb.AppendLine($"顧客{i}")
        Next

        Dim plan As CustomerImportPlan = Analyze(sb.ToString())

        Assert.IsEmpty(plan.Errors)
        Assert.HasCount(CustomerCsvImporter.MaxRows, plan.Customers)
    End Sub

    ' ---- 重複 ----

    <TestMethod>
    Public Sub Analyze_既に登録済のメールアドレスはスキップされる_エラーではない()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header,
            "山田,,taro@example.com,,,", "佐藤,,hanako@example.com,,,"), "taro@example.com")

        Assert.IsEmpty(plan.Errors)
        Assert.HasCount(1, plan.Skipped)
        Assert.AreEqual(2, plan.Skipped(0).LineNumber)
        AssertContains(plan.Skipped(0).Message, "既に登録")
        Assert.HasCount(1, plan.Customers)
        Assert.AreEqual("佐藤", plan.Customers(0).Name)
        Assert.IsTrue(plan.CanImport)
    End Sub

    <TestMethod>
    Public Sub Analyze_メールアドレスの大文字小文字の違いは同じとみなす()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, "山田,,Taro@Example.com,,,"), "taro@example.com")

        Assert.HasCount(1, plan.Skipped)
        Assert.IsEmpty(plan.Customers)
    End Sub

    <TestMethod>
    Public Sub Analyze_ファイル内の重複は後の行がスキップされ最初の行番号が示される()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header,
            "山田,,a@example.com,,,",
            "佐藤,,b@example.com,,,",
            "鈴木,,A@example.com,,,"))

        Assert.HasCount(2, plan.Customers)
        Assert.HasCount(1, plan.Skipped)
        Assert.AreEqual(4, plan.Skipped(0).LineNumber)
        AssertContains(plan.Skipped(0).Message, "2行目")
    End Sub

    <TestMethod>
    Public Sub Analyze_メールアドレスが空の行は重複の判定をしない()
        ' 同姓同名の別人がいるため、名前では重複を判定しない
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, "山田 太郎,,,,,", "山田 太郎,,,,,"))

        Assert.IsEmpty(plan.Skipped)
        Assert.HasCount(2, plan.Customers)
    End Sub

    <TestMethod>
    Public Sub Analyze_エラーのある行は重複の判定の対象にならない()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, ",,a@example.com,,,", "山田,,a@example.com,,,"))

        Assert.HasCount(1, plan.Errors)
        Assert.IsEmpty(plan.Skipped)
    End Sub

    ' ---- 結果の説明文 ----

    <TestMethod>
    Public Sub BuildReport_件数と文字コードが含まれる()
        Dim plan As CustomerImportPlan = Analyze(Csv(Header, "山田,,,,,"))

        Dim report As String = CustomerCsvImporter.BuildReport(plan, "UTF-8")

        AssertContains(report, "UTF-8")
        AssertContains(report, "取り込める: 1件")
    End Sub

    <TestMethod>
    Public Sub BuildReport_エラーが多いときは指定した件数で切って残りの件数を示す()
        Dim sb As New StringBuilder()
        sb.AppendLine("名前,メール")
        For i As Integer = 1 To 10
            ' 名前が空、メールの形式NG
            sb.AppendLine($",bad{i}")
        Next

        Dim plan As CustomerImportPlan = Analyze(sb.ToString())
        Dim report As String = CustomerCsvImporter.BuildReport(plan, Nothing, 5)

        Assert.HasCount(20, plan.Errors)
        AssertContains(report, "ほか 15件")
    End Sub

End Class