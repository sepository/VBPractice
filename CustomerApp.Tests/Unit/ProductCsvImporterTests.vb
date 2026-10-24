Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' 商品のCSVを検査
''' </summary>
<TestClass>
Public Class ProductCsvImporterTests

    Private Const Header As String = "商品コード,商品名,単価,販売中"

    Private Shared Function Csv(ParamArray lines As String()) As String
        Return String.Join(vbCrLf, lines)
    End Function

    Private Shared Function Analyze(csvText As String, ParamArray existingCodes As String()) As ProductImportPlan
        Return ProductCsvImporter.Analyze(csvText, existingCodes)
    End Function

    Private Shared Sub AssertSingleError(plan As ProductImportPlan, lineNumber As Integer, part As String)
        Assert.HasCount(1, plan.Errors, ProductCsvImporter.BuildReport(plan, Nothing))
        Assert.AreEqual(lineNumber, plan.Errors(0).LineNumber)
        Assert.IsGreaterThanOrEqualTo(0, plan.Errors(0).Message.IndexOf(part, StringComparison.Ordinal),
                                      $"{part}が含まれるはず: {plan.Errors(0).Message}")
    End Sub

    ' ---- 正常 ----

    <TestMethod>
    Public Sub Analyze_正しいファイルは取り込める()
        Dim plan As ProductImportPlan = Analyze(Csv(Header,
            "P0100,りんご,120,販売中",
            "P0101,みかん,80.5,販売停止"))

        Assert.IsEmpty(plan.Errors)
        Assert.IsEmpty(plan.Skipped)
        Assert.IsTrue(plan.CanImport)
        Assert.HasCount(2, plan.Products)
        Assert.AreEqual("P0100", plan.Products(0).ProductCode)
        Assert.AreEqual(120D, plan.Products(0).UnitPrice)
        Assert.IsTrue(plan.Products(0).IsActive)
        Assert.AreEqual(80.5D, plan.Products(1).UnitPrice)
        Assert.IsFalse(plan.Products(1).IsActive)
    End Sub

    <TestMethod>
    Public Sub Analyze_列の順番は自由で販売中の列は省略できる()
        Dim plan As ProductImportPlan = Analyze(Csv("単価,商品名,商品コード", "300,ぶどう,P0200"))

        Assert.IsEmpty(plan.Errors)
        Assert.HasCount(1, plan.Products)
        Assert.AreEqual("P0200", plan.Products(0).ProductCode)
        Assert.AreEqual(300D, plan.Products(0).UnitPrice)
        Assert.IsTrue(plan.Products(0).IsActive, "省略した場合は販売中")
    End Sub

    <TestMethod>
    Public Sub Analyze_英語の見出しも使える()
        Dim plan As ProductImportPlan = Analyze(Csv("code,name,price,active", "A1,Apple,100,0"))

        Assert.IsEmpty(plan.Errors)
        Assert.IsFalse(plan.Products(0).IsActive)
    End Sub

    <TestMethod>
    Public Sub Analyze_単価の書き方いろいろ()
        Dim plan As ProductImportPlan = Analyze(Csv(Header,
            "A1,a,""1,200"",",
            "A2,b,¥3500,",
            "A3,c,500円,",
            "A4,d,0,"))

        Assert.IsEmpty(plan.Errors, ProductCsvImporter.BuildReport(plan, Nothing))
        Assert.AreEqual(1200D, plan.Products(0).UnitPrice)
        Assert.AreEqual(3500D, plan.Products(1).UnitPrice)
        Assert.AreEqual(500D, plan.Products(2).UnitPrice)
        Assert.AreEqual(0D, plan.Products(3).UnitPrice, "0円は許可")
    End Sub

    <TestMethod>
    Public Sub Analyze_販売中の書き方いろいろ()
        Dim plan As ProductImportPlan = Analyze(Csv(Header,
            "A1,a,1,はい", "A2,b,1,停止", "A3,c,1,TRUE", "A4,d,1,No", "A5,e,1,×", "A6,f,1,"))

        Assert.IsEmpty(plan.Errors, ProductCsvImporter.BuildReport(plan, Nothing))
        Assert.IsTrue(plan.Products(0).IsActive)
        Assert.IsFalse(plan.Products(1).IsActive)
        Assert.IsTrue(plan.Products(2).IsActive)
        Assert.IsFalse(plan.Products(3).IsActive)
        Assert.IsFalse(plan.Products(4).IsActive)
        Assert.IsTrue(plan.Products(5).IsActive)
    End Sub

    ' ---- エラー ----

    <TestMethod>
    Public Sub Analyze_必須の列がないとエラー()
        Dim plan As ProductImportPlan = Analyze(Csv("商品コード,商品名", "A1,a"))

        Assert.HasCount(1, plan.Errors)
        Assert.AreEqual(1, plan.Errors(0).LineNumber)
        Assert.IsFalse(plan.CanImport)
    End Sub

    <TestMethod>
    Public Sub Analyze_コードと商品名が空だとエラー()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, ",a,1,", "A2,,1,"))

        Assert.HasCount(2, plan.Errors)
        Assert.AreEqual(2, plan.Errors(0).LineNumber)
        Assert.AreEqual(3, plan.Errors(1).LineNumber)
    End Sub

    <TestMethod>
    Public Sub Analyze_長すぎるコードはエラー()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, New String("X"c, ProductCsvImporter.MaxCodeLength + 1) & ",a,1,"))

        AssertSingleError(plan, 2, "商品コードが長すぎます。")
    End Sub

    <TestMethod>
    Public Sub Analyze_コードは20文字ちょうどなら通る()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, New String("X"c, ProductCsvImporter.MaxCodeLength) & ",a,1,"))

        Assert.IsEmpty(plan.Errors)
    End Sub

    <TestMethod>
    Public Sub Analyze_単価のエラーは行番号つきで全部報告される()
        Dim plan As ProductImportPlan = Analyze(Csv(Header,
            "A1,a,abc,",
            "A2,b,-1,",
            "A3,c,1.005",
            "A4,d,100000000D,",
            "A5,e,,",
            "A6,f,100,"))

        Assert.HasCount(5, plan.Errors, ProductCsvImporter.BuildReport(plan, Nothing))
        Assert.AreEqual(2, plan.Errors(0).LineNumber)
        Assert.AreEqual(6, plan.Errors(4).LineNumber)
        Assert.IsFalse(plan.CanImport, "1行でもエラーなら取り込まない")
    End Sub

    <TestMethod>
    Public Sub Analyze_上限ちょうどの単価は通る()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, "A1,a,99999999,"))

        Assert.IsEmpty(plan.Errors)
    End Sub

    <TestMethod>
    Public Sub Analyze_販売中の値が分からないとエラー()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, "A1,a,1,たぶん"))

        AssertSingleError(plan, 2, "販売中")
    End Sub

    <TestMethod>
    Public Sub Analyze_項目の数が合わない行はエラー()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, "A1,a,1"))

        AssertSingleError(plan, 2, "項目の数")
    End Sub

    ' ---- 重複 ----

    <TestMethod>
    Public Sub Analyze_登録済みのコードは大文字小文字を区別せずスキップされる()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, "p0001,既存,1,", "P0002,新規,1,"), "P0001")

        Assert.IsEmpty(plan.Errors)
        Assert.HasCount(1, plan.Skipped)
        Assert.AreEqual(2, plan.Skipped(0).LineNumber)
        Assert.HasCount(1, plan.Products)
        Assert.AreEqual("P0002", plan.Products(0).ProductCode)
    End Sub

    <TestMethod>
    Public Sub Analyze_ファイル内の重複は後の行がスキップされる()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, "A1,最初,100,", "A1,後,200,"))

        Assert.HasCount(1, plan.Products)
        Assert.AreEqual("最初", plan.Products(0).Name)
        Assert.HasCount(1, plan.Skipped)
        Assert.AreEqual(3, plan.Skipped(0).LineNumber)
    End Sub

    <TestMethod>
    Public Sub Analyze_エラーのある行は重複の判定に使われない()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, "A1,a,abc,", "A1,b,100,"))

        Assert.HasCount(1, plan.Errors)
        Assert.IsEmpty(plan.Skipped)
        Assert.HasCount(1, plan.Products)
    End Sub

    ' ---- ファイル全体 ----

    <TestMethod>
    Public Sub Analyze_空のファイルとヘッダーだけのファイルはエラー()
        Assert.HasCount(1, Analyze("").Errors)
        Assert.HasCount(1, Analyze(Header).Errors)
    End Sub

    <TestMethod>
    Public Sub BuildReport_件数とエラーの行が含まれる()
        Dim plan As ProductImportPlan = Analyze(Csv(Header, "A1,a,100,", "A2,b,xx,"))
        Dim report As String = ProductCsvImporter.BuildReport(plan, "UTF-8")

        Assert.IsGreaterThanOrEqualTo(0, report.IndexOf("3行目", StringComparison.Ordinal), report)
        Assert.IsGreaterThanOrEqualTo(0, report.IndexOf("UTF-8", StringComparison.Ordinal), report)
    End Sub

End Class
